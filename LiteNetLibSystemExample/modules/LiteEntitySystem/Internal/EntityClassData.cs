using System;
using System.Collections.Generic;
using System.Reflection;

namespace LiteEntitySystem.Internal
{
    internal readonly struct SyncableFieldInfo
    {
        public readonly int Offset;

        public SyncableFieldInfo(int offset)
        {
            Offset = offset;
        }
    }

    internal readonly struct RpcOffset
    {
        public readonly int SyncableOffset;
        public readonly int Offset;
        public readonly SyncFlags Flags;

        public RpcOffset(int syncableOffset, int offset, SyncFlags executeFlags)
        {
            SyncableOffset = syncableOffset;
            Offset = offset;
            Flags = executeFlags;
        }
    }

    internal struct EntityClassData
    {
        public bool IsRpcBound;
        public readonly ushort ClassId;
        public readonly ushort FilterId;
        public readonly bool IsSingleton;
        public readonly ushort[] BaseIds;
        public readonly int FieldsCount;
        public readonly int FieldsFlagsSize;
        public readonly int FixedFieldsSize;
        public readonly int PredictedSize;
        public readonly bool HasRemoteRollbackFields;
        public readonly EntityFieldInfo[] Fields;
        public readonly SyncableFieldInfo[] SyncableFields;
        public readonly RpcOffset[] RpcOffsets;
        public readonly int InterpolatedFieldsSize;
        public readonly int InterpolatedCount;
        public readonly EntityFieldInfo[] LagCompensatedFields;
        public readonly int LagCompensatedSize;
        public readonly int LagCompensatedCount;
        public readonly bool UpdateOnClient;
        public readonly bool IsUpdateable;
        public readonly bool IsLocalOnly;
        public readonly EntityConstructor<InternalEntity> EntityConstructor;
        public readonly MethodCallDelegate[] RemoteCallsClient;
        public readonly Delegate[] RemoteCallsServer;

        private readonly bool _isCreated;
        private readonly Type[] _baseTypes;

        private static readonly Type InternalEntityType = typeof(InternalEntity);
        private static readonly Type SingletonEntityType = typeof(SingletonEntityLogic);
        private static readonly Type SyncableFieldType = typeof(SyncableField);

        private static List<Type> GetBaseTypes(Type ofType, Type until, bool includeSelf)
        {
            var baseTypes = new List<Type>();
            var baseType = ofType.BaseType;
            while (baseType != until)
            {
                baseTypes.Insert(0, baseType);
                baseType = baseType!.BaseType;
            }
            if (includeSelf)
                baseTypes.Add(ofType);
            return baseTypes;
        }

        private static bool IsRemoteCallType(Type ft)
        {
            if (ft == typeof(RemoteCall))
                return true;
            if (!ft.IsGenericType)
                return false;
            var genericTypeDef = ft.GetGenericTypeDefinition();
            return genericTypeDef == typeof(RemoteCall<>) || genericTypeDef == typeof(RemoteCallSpan<>);
        }

        public EntityClassData(ushort filterId, Type entType, RegisteredTypeInfo typeInfo)
        {
            IsRpcBound = false;
            HasRemoteRollbackFields = false;
            PredictedSize = 0;
            FixedFieldsSize = 0;
            LagCompensatedSize = 0;
            LagCompensatedCount = 0;
            InterpolatedCount = 0;
            InterpolatedFieldsSize = 0;

            ClassId = typeInfo.ClassId;

            var updateAttribute = entType.GetCustomAttribute<UpdateableEntity>();
            if (updateAttribute != null)
            {
                IsUpdateable = true;
                UpdateOnClient = updateAttribute.UpdateOnClient;
            }
            else
            {
                IsUpdateable = false;
                UpdateOnClient = false;
            }

            IsLocalOnly = entType.GetCustomAttribute<LocalOnly>() != null;
            EntityConstructor = typeInfo.Constructor;
            IsSingleton = entType.IsSubclassOf(SingletonEntityType);
            FilterId = filterId;

            var baseTypes = GetBaseTypes(entType, InternalEntityType, false);
            _baseTypes = baseTypes.ToArray();
            BaseIds = new ushort[baseTypes.Count];

            var fields = new List<EntityFieldInfo>();
            var syncableFields = new List<SyncableFieldInfo>();
            var lagCompensatedFields = new List<EntityFieldInfo>();
            var remoteCallOffsets = new List<RpcOffset>();

            //add here to baseTypes to add fields
            baseTypes.Insert(0, typeof(InternalEntity));
            baseTypes.Add(entType);

            const BindingFlags bindingFlags = BindingFlags.Instance |
                                              BindingFlags.Public |
                                              BindingFlags.NonPublic |
                                              BindingFlags.DeclaredOnly;

            foreach (var baseType in baseTypes)
            {
                //cache fields
                foreach (var field in baseType.GetFields(bindingFlags))
                {
                    var ft = field.FieldType;

                    var syncVarFieldAttribute = field.GetCustomAttribute<SyncVarFlags>();
                    var syncVarClassAttribute = baseType.GetCustomAttribute<SyncVarFlags>();
                    var syncFlags = syncVarFieldAttribute?.Flags
                                 ?? syncVarClassAttribute?.Flags
                                 ?? SyncFlags.None;
                    int offset = Utils.GetFieldOffset(field);

                    if (IsRemoteCallType(ft))
                    {
                        remoteCallOffsets.Add(new RpcOffset(-1, offset, syncFlags));
                    }
                    //syncvars
                    else if (ft.IsValueType && ft.IsGenericType && !ft.IsArray)
                    {
                        FieldType internalFieldType;
                        var genericType = ft.GetGenericTypeDefinition();

                        if (genericType == typeof(SyncVar<>))
                            internalFieldType = FieldType.SyncVar;
                        else
                            continue;

                        ft = ft.GetGenericArguments()[0];

                        if (ft.IsEnum)
                            ft = ft.GetEnumUnderlyingType();

                        if (!ValueProcessors.RegisteredProcessors.TryGetValue(ft, out var valueTypeProcessor))
                        {
                            Logger.LogError($"Unregistered field type: {ft}");
                            continue;
                        }
                        int fieldSize = valueTypeProcessor.Size;
                        if (syncFlags.HasFlagFast(SyncFlags.Interpolated) && !ft.IsEnum)
                        {
                            InterpolatedFieldsSize += fieldSize;
                            InterpolatedCount++;
                        }
                        var fieldInfo = new EntityFieldInfo($"{baseType.Name}-{field.Name}", valueTypeProcessor, offset, internalFieldType, syncFlags);
                        if (syncFlags.HasFlagFast(SyncFlags.LagCompensated))
                        {
                            lagCompensatedFields.Add(fieldInfo);
                            LagCompensatedSize += fieldSize;
                        }
                        if (syncFlags.HasFlagFast(SyncFlags.AlwaysRollback))
                            HasRemoteRollbackFields = true;

                        if (fieldInfo.IsPredicted)
                            PredictedSize += fieldSize;

                        fields.Add(fieldInfo);
                        FixedFieldsSize += fieldSize;
                    }
                    else if (ft.IsSubclassOf(SyncableFieldType))
                    {
                        if (!field.IsInitOnly)
                            throw new Exception($"Syncable fields should be readonly! (Class: {entType} Field: {field.Name})");

                        syncableFields.Add(new SyncableFieldInfo(offset));
                        foreach (var syncableType in GetBaseTypes(ft, SyncableFieldType, true))
                        {
                            //syncable fields
                            foreach (var syncableField in syncableType.GetFields(bindingFlags))
                            {
                                var syncableFieldType = syncableField.FieldType;
                                if (IsRemoteCallType(syncableFieldType))
                                {
                                    int rpcOffset = Utils.GetFieldOffset(syncableField);
                                    remoteCallOffsets.Add(new RpcOffset(offset, rpcOffset, syncFlags));
                                    continue;
                                }
                                if (!syncableFieldType.IsValueType || !syncableFieldType.IsGenericType || syncableFieldType.GetGenericTypeDefinition() != typeof(SyncVar<>))
                                    continue;

                                syncableFieldType = syncableFieldType.GetGenericArguments()[0];
                                if (syncableFieldType.IsEnum)
                                    syncableFieldType = syncableFieldType.GetEnumUnderlyingType();

                                if (!ValueProcessors.RegisteredProcessors.TryGetValue(syncableFieldType, out var valueTypeProcessor))
                                {
                                    Logger.LogError($"Unregistered field type: {syncableFieldType}");
                                    continue;
                                }
                                int syncvarOffset = Utils.GetFieldOffset(syncableField);
                                var fieldInfo = new EntityFieldInfo($"{baseType.Name}-{field.Name}:{syncableField.Name}", valueTypeProcessor, offset, syncvarOffset, syncFlags);
                                fields.Add(fieldInfo);
                                FixedFieldsSize += fieldInfo.IntSize;
                                if (fieldInfo.IsPredicted)
                                    PredictedSize += fieldInfo.IntSize;
                            }
                        }
                    }
                }
            }

            //sort by placing interpolated first
            fields.Sort((a, b) =>
            {
                int wa = a.Flags.HasFlagFast(SyncFlags.Interpolated) ? 1 : 0;
                int wb = b.Flags.HasFlagFast(SyncFlags.Interpolated) ? 1 : 0;
                return wb - wa;
            });
            Fields = fields.ToArray();
            RpcOffsets = remoteCallOffsets.ToArray();
            SyncableFields = syncableFields.ToArray();
            FieldsCount = Fields.Length;
            FieldsFlagsSize = (FieldsCount - 1) / 8 + 1;
            LagCompensatedFields = lagCompensatedFields.ToArray();
            LagCompensatedCount = LagCompensatedFields.Length;
            RemoteCallsClient = new MethodCallDelegate[RpcOffsets.Length];
            RemoteCallsServer = new Delegate[RpcOffsets.Length];

            int fixedOffset = 0;
            int predictedOffset = 0;
            for (int i = 0; i < Fields.Length; i++)
            {
                ref var field = ref Fields[i];
                field.FixedOffset = fixedOffset;
                fixedOffset += field.IntSize;
                if (field.IsPredicted)
                {
                    field.PredictedOffset = predictedOffset;
                    predictedOffset += field.IntSize;
                }
                else
                {
                    field.PredictedOffset = -1;
                }
            }

            _isCreated = true;
        }

        public void PrepareBaseTypes(Dictionary<Type, ushort> registeredTypeIds, ref ushort singletonCount, ref ushort filterCount)
        {
            if (!_isCreated)
                return;
            for (int i = 0; i < BaseIds.Length; i++)
            {
                if (!registeredTypeIds.TryGetValue(_baseTypes[i], out BaseIds[i]))
                {
                    BaseIds[i] = IsSingleton
                        ? singletonCount++
                        : filterCount++;
                    registeredTypeIds.Add(_baseTypes[i], BaseIds[i]);
                }
                //Logger.Log($"Base type of {classData.ClassId} - {baseTypes[i]}");
            }
        }
    }

    // ReSharper disable once UnusedTypeParameter
    internal static class EntityClassInfo<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        internal static ushort ClassId;
    }
}