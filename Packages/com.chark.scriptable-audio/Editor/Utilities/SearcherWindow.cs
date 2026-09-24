using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CHARK.ScriptableAudio.Editor.Utilities
{
    /// <summary>
    /// Search window for Scriptable Event assets.
    /// </summary>
    internal abstract class SearcherWindow<TData> : ScriptableObject, ISearchWindowProvider
    {
        /// <summary>
        /// Invoked when a data entry is clicked.
        /// </summary>
        public event Action<TData> OnClicked;

        /// <summary>
        /// Show this window.
        /// </summary>
        public void Show()
        {
            var screenMousePosition = AudioEditorGUI.GetScreenMousePosition();
            var searchContext = new SearchWindowContext(screenMousePosition);

            SearchWindow.Open(searchContext, this);
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            return GetSortedEntries();
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is TData data)
            {
                OnClicked?.Invoke(data);
            }

            return true;
        }

        protected virtual IEnumerable<TData> GetData()
        {
            return Array.Empty<TData>();
        }

        protected virtual string GetNoEntriesTitle()
        {
            return "No Entries";
        }

        protected virtual string GetTitle()
        {
            return "Searcher";
        }

        protected virtual string GetName(TData data)
        {
            return data.ToString();
        }

        protected virtual Texture GetIcon(TData data)
        {
            if (!(data is Object dataObject))
            {
                return null;
            }

            var content = AudioEditorGUI.GetObjectContent(dataObject);
            var image = content?.image;

            return image;
        }

        protected virtual IEnumerable<string> GetPath(TData data)
        {
            var dataName = GetName(data);
            if (string.IsNullOrWhiteSpace(dataName))
            {
                return Array.Empty<string>();
            }

            return dataName.Split('/');
        }

        private List<SearchTreeEntry> GetSortedEntries()
        {
            var entriesByPath = GetEntriesByPath();
            var sortedEntries = entriesByPath.Values
                .SelectMany(entries => entries.OrderBy(entry => entry.name))
                .ToList();

            var rootEntryTitle = sortedEntries.Count == 0
                ? GetNoEntriesTitle()
                : GetTitle();

            var rootEntry = CreateGroupEntry(rootEntryTitle, 0);

            return sortedEntries.Prepend(rootEntry).ToList();
        }

        private SortedDictionary<string, List<SearchTreeEntry>> GetEntriesByPath()
        {
            var entriesByPath = new SortedDictionary<string, List<SearchTreeEntry>>();

            var dataEntries = GetData();
            foreach (var data in dataEntries)
            {
                var dataName = GetName(data);
                var dataIcon = GetIcon(data);
                var dataPath = GetPath(data).ToList();

                var groupName = string.Empty;
                for (var index = 0; index < dataPath.Count - 1; index++)
                {
                    var dataPathPart = dataPath[index];
                    groupName += dataPathPart;

                    if (entriesByPath.ContainsKey(groupName) == false)
                    {
                        var groupEntry = CreateGroupEntry(dataPathPart, index + 1);
                        entriesByPath.Add(groupName, new List<SearchTreeEntry> {groupEntry});
                    }

                    groupName += "/";
                }

                if (entriesByPath.TryGetValue(groupName, out var entries) == false)
                {
                    entries = new List<SearchTreeEntry>();
                    entriesByPath.Add(groupName, entries);
                }

                var entry = CreateEntry(dataName, dataIcon, data, dataPath.Count);
                entries.Add(entry);
            }

            return entriesByPath;
        }

        private static SearchTreeGroupEntry CreateGroupEntry(string name, int level)
        {
            var entryContent = new GUIContent(name);
            var entry = new SearchTreeGroupEntry(entryContent)
            {
                level = level
            };

            return entry;
        }

        private static SearchTreeEntry CreateEntry(string name, Texture icon, TData data, int level)
        {
            var entryContent = new GUIContent(name, icon);
            var entry = new SearchTreeEntry(entryContent)
            {
                userData = data,
                level = level
            };

            return entry;
        }
    }
}
