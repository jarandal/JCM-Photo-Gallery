using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace JMC_Photo_Gallery
{
    public class ObservableCollection2D<T, T2, T3>
    {
        private Dictionary<T, Dictionary<T2, T3>> _content = new Dictionary<T, Dictionary<T2, T3>>();
        private ObservableCollection<T3> _output = new ObservableCollection<T3>();
        public ObservableCollection<T3> Output { get { return _output; } }

        public ObservableCollection2D()
        {
        }

        public ObservableCollection2D(ObservableCollection<T3> linkOutput)
        {
            _output = linkOutput;
        }

        public int Count
        {
            get
            {
                int result = 0;
                foreach (Dictionary<T2, T3> item in _content.Values)
                    result += item.Count;
                return result;
            }
        }

        public int CountIn(T key1)
        {
            if (HasKey(key1))
                return _content[key1].Count;
            return 0;
        }

        public bool HasKey(T key1)
        {
            return _content.ContainsKey(key1);
        }

        public bool HasKey(T key1, T2 key2)
        {
            if (HasKey(key1))
                return _content[key1].ContainsKey(key2);
            else
                return false;
        }

        // will overwrite value with same keys
        public void Set(T key1, T2 key2, T3 item)
        {
            if (_content.ContainsKey(key1) && _content[key1].ContainsKey(key2))
            {
                // Add to ObservableCollection
                int index = _output.IndexOf(_content[key1][key2]);
                _output[index] = item;

                // Add to Dictionary
                _content[key1][key2] = item;
            }
            else if (_content.ContainsKey(key1))
            {
                // Add to ObservableCollection
                Dictionary<T2, T3> subDictionary = _content[key1];
                T3[] tempArray = new T3[subDictionary.Count];
                subDictionary.Values.CopyTo(tempArray, 0);
                int indexPreviousItem = _output.IndexOf(tempArray[tempArray.Length - 1]);
                if (indexPreviousItem + 1 == _output.Count)
                    _output.Add(item);
                else
                    _output.Insert(indexPreviousItem + 1, item);

                // Add to Dictionary
                subDictionary.Add(key2, item);
            }
            else
            {
                // Add to ObservableCollection
                _output.Add(item);

                // Add to Dictionary
                Dictionary<T2, T3> newSubDictionary = new Dictionary<T2, T3>();
                newSubDictionary.Add(key2, item);
                _content.Add(key1, newSubDictionary);
            }
        }

        public void Remove(T key1)
        {
            if (HasKey(key1))
            {
                // Remove From ObservableCollection
                Dictionary<T2, T3> dictionary = _content[key1];
                foreach (T2 key2 in dictionary.Keys)
                    Remove(key1, key2);

                // Remove From Dictionary
                _content.Remove(key1);
            }
        }

        public void Remove(T key1, T2 key2)
        {
            if (HasKey(key1, key2))
            {
                // Remove From ObservableCollection
                _output.Remove(_content[key1][key2]);

                // Remove From Dictionary
                _content[key1].Remove(key2);
            }
        }

        public Dictionary<T2, T3> Get(T key1)
        {
            return _content[key1];
        }

        public T3 Get(T key1, T2 key2)
        {
            return _content[key1][key2];
        }

    }
}
