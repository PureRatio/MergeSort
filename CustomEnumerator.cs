using System;
using System.Collections.Generic;

namespace FileSort
{
    public class CustomEnumerator<T> : IEnumerator<T>
    {
        private IEnumerator<T> internalEnum;

        public bool HasEnded { get; protected set; }

        public CustomEnumerator(IEnumerator<T> enumerator)
        {
            internalEnum = enumerator;
        }

        public T Current
        {
            get { return internalEnum.Current; }
        }

        public void Dispose()
        {
            internalEnum.Dispose();
        }

        object System.Collections.IEnumerator.Current
        {
            get { return internalEnum.Current; }
        }

        public bool MoveNext()
        {
            bool moved = internalEnum.MoveNext();
            if (!moved)
                HasEnded = true;
            return moved;
        }

        public void Reset()
        {
            internalEnum.Reset();
            HasEnded = false;
        }
    }
}
