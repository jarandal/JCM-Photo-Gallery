using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace JMC_Photo_Gallery
{
    public class CollectionRound<T> : Collection<T>
    {
        public T GetRound(int index)
        {
            index %= this.Count;
            if(index < 0)
                index += this.Count;
            return this[index];
        }
    }
}
