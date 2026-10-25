// =============================================================================
// SearchController.cs — App search
// =============================================================================
using UnityEngine;
using TMPro;

namespace VisionSpace.Launcher
{
    public class SearchController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private AppGridController appGrid;

        private string _lastQuery = "";

        private void Update()
        {
            if (searchInput == null || appGrid == null) return;

            string query = searchInput.text;
            if (query != _lastQuery)
            {
                _lastQuery = query;
                appGrid.SetSearchQuery(query);
            }
        }

        public void ClearSearch()
        {
            if (searchInput != null) searchInput.text = "";
            _lastQuery = "";
            appGrid?.SetSearchQuery("");
        }
    }
}
