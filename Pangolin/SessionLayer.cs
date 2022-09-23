using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Pangolin
{
    public class SessionLayer //: IDictionary<string, dynamic>
    {
        private static Dictionary<string, dynamic> _dictionary = new Dictionary<string, dynamic>(10, StringComparer.InvariantCulture);
        private static ListDictionary _listDictionary = new ListDictionary(StringComparer.InvariantCulture);

        #region IDictionary Properties
        public int Count { get; private set; } = 0;
        
        public bool IsReadOnly { get; }

        public ICollection<string> Keys
        {
            get
            {
                if (Count > 9)
                {
                    return _dictionary.Keys;
                }
                else
                {
                    return _listDictionary.Keys as ICollection<string>;
                }
            }
        }

        public ICollection<dynamic> Values
        {
            get
            {
                if (Count > 9)
                {
                    return _dictionary.Values;
                }
                else
                {
                    return _listDictionary.Values as ICollection<dynamic>;
                }
            }
        }
        #endregion

        private bool UsingDictionary => Count > 9;

        public static SessionLayer Instance { get; } = new SessionLayer();

        private SessionLayer() { }

        #region IDictionary Methods
        public object this[string key]
        {
            get
            {
                if (Count > 9)
                {
                    return _dictionary[key];
                }
                else
                {
                    return _listDictionary[key];
                }
            }
            set
            {
                Add(key, value);
            }
        }

        public void Add(string key, dynamic value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (_listDictionary.Contains(key) || _dictionary.ContainsKey(key))
            {
                throw new ArgumentException("key already added to the collection.");
            }

            if (Count == 9)
            {
                foreach (DictionaryEntry dictionaryEntry in _listDictionary)
                {
                    _dictionary.Add(dictionaryEntry.Key.ToString(), dictionaryEntry.Value);
                }
                _listDictionary.Clear();

                _dictionary.Add(key, value);

                Count = _dictionary.Count;
            }
            else if (Count < 9)
            {
                _listDictionary.Add(key, value);
                Count = _listDictionary.Count;
            }
            else
            {
                _dictionary.Add(key, value);
                Count = _dictionary.Count;
            }
        }

        public void Add(KeyValuePair<string, dynamic> keyValuePair)
        {
            if (Count == 9)
            {
                foreach (DictionaryEntry dictionaryEntry in _listDictionary)
                {
                    _dictionary.Add(dictionaryEntry.Key.ToString(), dictionaryEntry.Value);
                }
                _listDictionary.Clear();

                _dictionary.Add(keyValuePair.Key, keyValuePair.Value);

                Count = _dictionary.Count;
            }
            else if (Count < 9)
            {
                _listDictionary.Add(keyValuePair.Key, keyValuePair.Value);
                Count = _listDictionary.Count;
            }
            else
            {
                _dictionary.Add(keyValuePair.Key, keyValuePair.Value);
                Count = _dictionary.Count;
            }
        }
        
        public void Clear()
        {
            if (_listDictionary?.Count > 0)
            {
                _listDictionary.Clear();
            }
            if (_dictionary?.Count > 0)
            {
                _dictionary.Clear();
            }

            Count = 0;
        }

        public bool Contains(KeyValuePair<string, dynamic> keyValuePair)
        {
            bool doesContain = false;

            if (Count > 9)
            {
                doesContain = _dictionary.TryGetValue(keyValuePair.Key, out dynamic containedValue) && containedValue == keyValuePair.Value;
            }
            else
            {
                foreach (DictionaryEntry dictionaryEntry in _listDictionary)
                {
                    if (dictionaryEntry.Key.Equals(keyValuePair.Key) && dictionaryEntry.Value.Equals(keyValuePair.Value))
                    {
                        doesContain = true;
                        break;
                    }
                }
            }

            return doesContain;
        }

        public bool ContainsKey(string key)
        {
            return Count > 9 ? _dictionary.ContainsKey(key) : _listDictionary.Contains(key);
        }

        public void CopyTo(KeyValuePair<string, dynamic>[] keyValuePairs, int index)
        {
            if (keyValuePairs == null)
            {
                throw new ArgumentNullException(nameof(keyValuePairs));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, nameof(index) + " cannot be less than zero.");
            }
            if ((index + Count) > keyValuePairs.Length)
            {
                throw new ArgumentException("Source length with index is greater than destination length.");
            }

            if (Count > 9)
            {
                foreach (KeyValuePair<string, dynamic> keyValue in _dictionary)
                {

                }
            }
            else
            {
                foreach (DictionaryEntry dictionaryEntry in _listDictionary)
                {

                }
            }

            throw new NotImplementedException("CopyTo");
        }
        
        public bool TryGetValue(string key, out dynamic value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            bool containsValue = false;

            value = default;

            if (Count > 9)
            {
                containsValue = _dictionary.TryGetValue(key, out value);
            }
            else
            {
                foreach (DictionaryEntry dictionaryEntry in _listDictionary)
                {
                    if (dictionaryEntry.Key.Equals(key))
                    {
                        value = dictionaryEntry.Value;
                        containsValue = true;
                        break;
                    }
                }
            }

            return containsValue;
        }

        public bool Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            bool isSuccessful = false;

            if (Count == 10)
            {
                isSuccessful = _dictionary.Remove(key);

                foreach (KeyValuePair<string, dynamic> keyValuePair in _dictionary)
                {
                    _listDictionary.Add(keyValuePair.Key, keyValuePair.Value);
                }
                _dictionary.Clear();

                Count = _listDictionary.Count;
            }
            else if (Count < 9)
            {
                _listDictionary.Remove(key);
                isSuccessful = true;
                Count = _listDictionary.Count;
            }
            else
            {
                isSuccessful = _dictionary.Remove(key);
                Count = _dictionary.Count;
            }

            return isSuccessful;
        }

        public bool Remove(KeyValuePair<string, dynamic> keyValuePair)
        {
            bool isSuccessful = false;

            if (Count == 10)
            {
                isSuccessful = _dictionary.Remove(keyValuePair.Key);

                foreach (KeyValuePair<string, dynamic> keyValue in _dictionary)
                {
                    _listDictionary.Add(keyValue.Key, keyValue.Value);
                }
                _dictionary.Clear();

                Count = _listDictionary.Count;
            }
            else if (Count < 9)
            {
                _listDictionary.Remove(keyValuePair.Key);
                isSuccessful = true;
                Count = _listDictionary.Count;
            }
            else
            {
                isSuccessful = _dictionary.Remove(keyValuePair.Key);
                Count = _dictionary.Count;
            }

            return isSuccessful;
        }

        //private class SessionLayerEnumerator : IDictionaryEnumerator
        //{

        //}
        #endregion
        
        public bool ContainsValue(dynamic value)
        {
            bool doesContain = false;

            if (Count > 9)
            {
                doesContain = _dictionary.ContainsValue(value);
            }
            else
            {
                foreach (DictionaryEntry dictionaryEntry in _listDictionary)
                {
                    if (dictionaryEntry.Value.Equals(value))
                    {
                        doesContain = true;
                        break;
                    }
                }
            }

            return doesContain;
        }

        public int IndexOf(dynamic value)
        {
            if (Count <= 0)
            {
                throw new InvalidOperationException("There are no variables stored in the SessionLayer.");
            }

            int index = default;

            if (Count >= 10)
            {
                foreach (KeyValuePair<string, dynamic> keyValuePair in _dictionary)
                {
                    if (object.Equals(value, keyValuePair.Value))
                    {
                        return index;
                    }

                    index++;
                }
            }
            else
            {
                foreach (DictionaryEntry dictionaryEntry in _listDictionary)
                {
                    if (object.Equals(value, dictionaryEntry.Value))
                    {
                        return index;
                    }

                    index++;
                }
            }

            return -1;
        }
    }
}
