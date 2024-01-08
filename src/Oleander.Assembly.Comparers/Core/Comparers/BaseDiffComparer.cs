namespace Oleander.Assembly.Comparers.Core.Comparers
{
    abstract class BaseDiffComparer<T> where T : class
    {
        public IEnumerable<IDiffItem> GetMultipleDifferences(IEnumerable<T> oldElements, IEnumerable<T> newElements)
        {
            List<T> oldElementsSorted = new List<T>(oldElements);
            oldElementsSorted.Sort(this.CompareElements);

            List<T> newElementsSorted = new List<T>(newElements);
            newElementsSorted.Sort(this.CompareElements);

            int oldIndex;
            int newIndex;

            List<IDiffItem> result = new List<IDiffItem>();

            for (oldIndex = 0, newIndex = 0; oldIndex < oldElementsSorted.Count && newIndex < newElementsSorted.Count; )
            {
                T oldElement = oldElementsSorted[oldIndex];
                T newElement = newElementsSorted[newIndex];

                int compareResult = this.CompareElements(oldElement, newElement);

                if (compareResult < 0)
                {
                    oldIndex++;
                    if (this.IsAPIElement(oldElement))
                    {
                        result.Add(this.GetMissingDiffItem(oldElement));
                    }
                }
                else if (compareResult > 0)
                {
                    newIndex++;
                    if (this.IsAPIElement(newElement))
                    {
                        IDiffItem newItem = this.GetNewDiffItem(newElement);
                        if (newItem != null)
                        {
                            result.Add(newItem);
                        }
                    }
                }
                else
                {
                    oldIndex++;
                    newIndex++;
                    if (this.IsAPIElement(oldElement) || this.IsAPIElement(newElement))
                    {
                        IDiffItem diffResult = this.GenerateDiffItem(oldElement, newElement);
                        if (diffResult != null)
                        {
                            result.Add(diffResult);
                        }
                    }
                }
            }

            for (; oldIndex < oldElementsSorted.Count; oldIndex++)
            {
                if (this.IsAPIElement(oldElementsSorted[oldIndex]))
                {
                    result.Add(this.GetMissingDiffItem(oldElementsSorted[oldIndex]));
                }
            }

            for (; newIndex < newElementsSorted.Count; newIndex++)
            {
                if (this.IsAPIElement(newElementsSorted[newIndex]))
                {
                    IDiffItem newItem = this.GetNewDiffItem(newElementsSorted[newIndex]);
                    if (newItem != null)
                    {
                        result.Add(newItem);
                    }
                }
            }

            return result;
        }

        protected abstract IDiffItem GetMissingDiffItem(T element);

        protected abstract IDiffItem GenerateDiffItem(T oldElement, T newElement);

        protected abstract IDiffItem GetNewDiffItem(T element);

        protected abstract bool IsAPIElement(T element);

        protected abstract int CompareElements(T x, T y);
    }
}
