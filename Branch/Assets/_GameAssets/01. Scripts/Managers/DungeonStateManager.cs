using System.Collections.Generic;

namespace Managers
{
    public class DungeonStateManager: Singleton<DungeonStateManager>
    {
        private Dictionary<int, bool> _objStates = new ();
        
        public void SetState(int objId, bool state)
        {
            if (_objStates.ContainsKey(objId))
                _objStates[objId] = state;
            else
            {
                _objStates.Add(objId, state);
            }
        }
        
        public bool GetState(int objId)
        {
            return _objStates.TryGetValue(objId, out bool state) && state;
        }
        
        public void ClearStates()
        {
            _objStates.Clear();
        }
    }
}