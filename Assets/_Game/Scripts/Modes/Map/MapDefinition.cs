using System;
using System.Collections.Generic;
using _Game.Scripts.Modes.Levels;
using UnityEngine;

namespace _Game.Scripts.Modes.Map
{
    [CreateAssetMenu(fileName = "Map_Arcade", menuName = "_Game/Arcade/Map Definition")]
    public class MapDefinition : ScriptableObject
    {
        #region Fields
        [SerializeField] private List<MapWorldDefinition> _worlds = new List<MapWorldDefinition>();
        [SerializeField] private List<MapLevelNodeDefinition> _nodes = new List<MapLevelNodeDefinition>();
        #endregion

        #region Properties
        public IReadOnlyList<MapWorldDefinition> Worlds => _worlds;
        public IReadOnlyList<MapLevelNodeDefinition> Nodes => _nodes;
        #endregion

        #region Lookup
        public MapLevelNodeDefinition FindNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return null;
            return _nodes.Find(node => node != null && node.NodeId == nodeId);
        }

        public MapLevelNodeDefinition FindNodeByLevel(LevelDefinition level)
        {
            if (level == null) return null;
            return _nodes.Find(node => node != null && node.Level == level);
        }
        #endregion
    }

    [Serializable]
    public class MapWorldDefinition
    {
        public string worldId = "world_01";
        public string displayName = "World 1";
        public Sprite backgroundSprite;
    }

    [Serializable]
    public class MapLevelNodeDefinition
    {
        #region Fields
        [SerializeField] private string _nodeId = "node_001";
        [SerializeField] private LevelDefinition _level;
        [SerializeField] private Vector2 _anchoredPosition;
        [SerializeField] private List<string> _requiredCompletedNodeIds = new List<string>();
        #endregion

        #region Properties
        public string NodeId => string.IsNullOrWhiteSpace(_nodeId) ? (_level != null ? _level.LevelId : string.Empty) : _nodeId;
        public LevelDefinition Level => _level;
        public Vector2 AnchoredPosition => _anchoredPosition;
        public IReadOnlyList<string> RequiredCompletedNodeIds => _requiredCompletedNodeIds;
        public string DisplayName => _level != null ? _level.DisplayName : NodeId;
        #endregion
    }
}
