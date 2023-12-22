using LiteEntitySystem.Internal;
using System;
using System.Collections.Generic;

namespace LiteEntitySystem
{
    /// <summary>
    /// Entity has update method
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateableEntity : Attribute
    {
        public readonly bool UpdateOnClient;

        public UpdateableEntity() { }

        public UpdateableEntity(bool updateOnClient)
        {
            UpdateOnClient = updateOnClient;
        }
    }

    /// <summary>
    /// Entity is local only (only on server or client no difference)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LocalOnly : Attribute { }

    /// <summary>
    /// Base class for simple (not controlled by controller) entity
    /// </summary>
    public abstract class EntityLogic : InternalEntity
    {
        //It should be in such order because later it checks rollbacks
        [SyncVarFlags(SyncFlags.NeverRollBack)]
        internal SyncVar<byte> InternalOwnerId;

        [SyncVarFlags(SyncFlags.NeverRollBack)]
        private SyncVar<EntitySharedReference> _parentId;

        /// <summary>
        /// Child entities (can be used for transforms or as components)
        /// </summary>
        public readonly HashSet<EntityLogic> Childs = new HashSet<EntityLogic>();

        /// <summary>
        /// Owner player id
        /// </summary>
        public byte OwnerId => InternalOwnerId;

        public EntitySharedReference ParentId => _parentId;

        private readonly byte[] _history;
        private readonly EntityFieldInfo[] _lagCompensatedFields;
        private readonly int _lagCompensatedSize;
        private readonly int _lagCompensatedCount;
        private int _filledHistory;
        private bool _lagCompensationEnabled;

        public bool HasLagCompensation => _lagCompensatedSize > 0;

        public EntitySharedReference SharedReference => new EntitySharedReference(this);

        internal unsafe void WriteHistory(ushort tick)
        {
            byte maxHistory = (byte)EntityManager.MaxHistorySize;
            _filledHistory = Math.Min(_filledHistory + 1, maxHistory);
            int historyOffset = ((tick % maxHistory) + 1) * _lagCompensatedSize;
            fixed (byte* history = _history)
            {
                for (int i = 0; i < _lagCompensatedCount; i++)
                {
                    ref var field = ref _lagCompensatedFields[i];
                    field.TypeProcessor.WriteTo(this, field.Offset, history + historyOffset);
                    historyOffset += field.IntSize;
                }
            }
        }

        //on client it works only in rollback
        internal unsafe void EnableLagCompensation(NetPlayer player)
        {
            if (_lagCompensationEnabled || IsControlledBy(player.Id))
                return;
            ushort tick = EntityManager.IsClient ? ClientManager.ServerTick : EntityManager.Tick;
            byte maxHistory = (byte)EntityManager.MaxHistorySize;
            if (Utils.SequenceDiff(player.StateATick, tick) >= 0 || Utils.SequenceDiff(player.StateBTick, tick) > 0)
            {
                Logger.Log($"LagCompensationMiss. Tick: {tick}, StateA: {player.StateATick}, StateB: {player.StateBTick}");
                return;
            }
            int historyAOffset = ((player.StateATick % maxHistory) + 1) * _lagCompensatedSize;
            int historyBOffset = ((player.StateBTick % maxHistory) + 1) * _lagCompensatedSize;
            int historyCurrent = 0;

            fixed (byte* history = _history)
            {
                for (int i = 0; i < _lagCompensatedCount; i++)
                {
                    ref var field = ref _lagCompensatedFields[i];
                    field.TypeProcessor.LoadHistory(
                        this,
                        field.Offset,
                        history + historyCurrent,
                        history + historyAOffset,
                        history + historyBOffset,
                        player.LerpTime);
                    historyAOffset += field.IntSize;
                    historyBOffset += field.IntSize;
                    historyCurrent += field.IntSize;
                }
            }

            OnLagCompensationStart();
            _lagCompensationEnabled = true;
        }

        internal unsafe void DisableLagCompensation()
        {
            if (!_lagCompensationEnabled)
                return;
            _lagCompensationEnabled = false;
            int historyOffset = 0;
            fixed (byte* history = _history)
            {
                for (int i = 0; i < _lagCompensatedCount; i++)
                {
                    ref var field = ref _lagCompensatedFields[i];
                    field.TypeProcessor.SetFrom(this, field.Offset, history + historyOffset);
                    historyOffset += field.IntSize;
                }
            }
            OnLagCompensationEnd();
        }

        /// <summary>
        /// Enable lag compensation for player that owns this entity
        /// </summary>
        public void EnableLagCompensationForOwner()
        {
            if (InternalOwnerId.Value == EntityManager.ServerPlayerId)
                return;
            EntityManager.EnableLagCompensation(EntityManager.IsClient
                ? ClientManager.LocalPlayer
                : ServerManager.GetPlayer(InternalOwnerId));
        }

        /// <summary>
        /// Disable lag compensation for player that owns this entity
        /// </summary>
        public void DisableLagCompensationForOwner()
        {
            EntityManager.DisableLagCompensation();
        }

        public int GetFrameSeed()
        {
            return EntityManager.IsClient
                ? (EntityManager.InRollBackState ? ClientManager.RollBackTick : EntityManager.Tick)
                : (InternalOwnerId.Value == EntityManager.ServerPlayerId ? EntityManager.Tick : ServerManager.GetPlayer(InternalOwnerId).LastProcessedTick);
        }

        /// <summary>
        /// Create predicted entity (like projectile) that will be replaced by server entity if prediction is successful
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="initMethod">Method that will be called after entity constructed</param>
        /// <returns>Created predicted local entity</returns>
        public T AddPredictedEntity<T>(Action<T> initMethod = null) where T : EntityLogic
        {
            if (EntityManager.IsServer)
            {
                if (InternalOwnerId.Value == EntityManager.ServerPlayerId)
                {
                    return ServerManager.AddEntity(initMethod);
                }

                var predictedEntity = ServerManager.AddEntity(initMethod);
                var player = ServerManager.GetPlayer(InternalOwnerId);
                ushort playerServerTick = player.SimulatedServerTick;
                while (playerServerTick != ServerManager.Tick)
                {
                    predictedEntity.Update();
                    playerServerTick++;
                }

                return predictedEntity;
            }

            var entity = EntityManager.AddLocalEntity(initMethod);
            ClientManager.AddPredictedInfo(entity);
            return entity;
        }

        /// <summary>
        /// Set parent entity
        /// </summary>
        /// <param name="parentEntity">parent entity</param>
        public void SetParent(EntityLogic parentEntity)
        {
            if (EntityManager.IsClient)
                return;

            var id = new EntitySharedReference(parentEntity);
            if (id == _parentId.Value)
                return;

            EntitySharedReference oldId = _parentId;
            _parentId = id;
            OnParentChange(oldId);

            var newParent = EntityManager.GetEntityById<EntityLogic>(_parentId)?.InternalOwnerId ?? EntityManager.ServerPlayerId;
            if (InternalOwnerId.Value != newParent.Value)
            {
                SetOwner(this, newParent);
            }
        }

        /// <summary>
        /// Get parent entity
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <returns>parent entity</returns>
        public T GetParent<T>() where T : EntityLogic
        {
            return EntityManager.GetEntityById<T>(_parentId);
        }

        /// <summary>
        /// Called when lag compensation was started for this entity
        /// </summary>
        protected virtual void OnLagCompensationStart()
        {

        }

        /// <summary>
        /// Called when lag compensation ended for this entity
        /// </summary>
        protected virtual void OnLagCompensationEnd()
        {

        }

        internal override bool IsControlledBy(byte playerId)
        {
            return playerId == InternalOwnerId.Value;
        }

        internal override void DestroyInternal()
        {
            if (IsDestroyed)
                return;
            base.DestroyInternal();
            if (EntityManager.IsClient && IsLocalControlled && !IsLocal)
            {
                ClientManager.RemoveOwned(this);
            }
            var parent = EntityManager.GetEntityById<EntityLogic>(_parentId);
            if (parent != null && !parent.IsDestroyed)
            {
                parent.Childs.Remove(this);
            }
            foreach (var entityLogic in Childs)
            {
                entityLogic.Destroy();
            }
        }

        private void OnOwnerChange(byte prevOwner)
        {
            if (IsLocalControlled && !IsLocal)
                ClientManager.AddOwned(this);
            else if (prevOwner == EntityManager.InternalPlayerId && !IsLocal)
                ClientManager.RemoveOwned(this);
        }

        private void OnParentChange(EntitySharedReference oldId)
        {
            EntityManager.GetEntityById<EntityLogic>(oldId)?.Childs.Remove(this);
            EntityManager.GetEntityById<EntityLogic>(_parentId)?.Childs.Add(this);
        }

        internal static void SetOwner(EntityLogic entity, byte ownerId)
        {
            entity.InternalOwnerId = ownerId;
            foreach (var child in entity.Childs)
            {
                SetOwner(child, ownerId);
            }
        }

        protected override void RegisterRPC(in RPCRegistrator r)
        {
            base.RegisterRPC(in r);
            r.BindOnChange(this, ref _parentId, OnParentChange);
            r.BindOnChange(this, ref InternalOwnerId, OnOwnerChange);
        }

        protected EntityLogic(EntityParams entityParams) : base(entityParams)
        {
            ref var classData = ref GetClassData();
            _lagCompensatedSize = classData.LagCompensatedSize;
            if (_lagCompensatedSize > 0)
            {
                _history = new byte[((byte)EntityManager.MaxHistorySize + 1) * _lagCompensatedSize];
                _lagCompensatedFields = classData.LagCompensatedFields;
                _lagCompensatedCount = classData.LagCompensatedCount;
            }
        }
    }
}