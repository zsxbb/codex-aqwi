using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CompilerGenerated]
internal sealed class <>z__ReadOnlyArray<T> : IEnumerable, ICollection, IList, IEnumerable<!0>, IReadOnlyCollection<!0>, IReadOnlyList<!0>, ICollection<!0>, IList<!0>
{
	public <>z__ReadOnlyArray(T[] items)
	{
		this._items = items;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this._items.GetEnumerator();
	}

	int ICollection.Count
	{
		get
		{
			return this._items.Length;
		}
	}

	bool ICollection.IsSynchronized
	{
		get
		{
			return false;
		}
	}

	object ICollection.SyncRoot
	{
		get
		{
			return this;
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		this._items.CopyTo(array, index);
	}

	object IList.this[int index]
	{
		get
		{
			return this._items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize
	{
		get
		{
			return true;
		}
	}

	bool IList.IsReadOnly
	{
		get
		{
			return true;
		}
	}

	int IList.Add(object value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object value)
	{
		return this._items.Contains(value);
	}

	int IList.IndexOf(object value)
	{
		return this._items.IndexOf(value);
	}

	void IList.Insert(int index, object value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	IEnumerator<T> IEnumerable<!0>.GetEnumerator()
	{
		return this._items.GetEnumerator();
	}

	int IReadOnlyCollection<!0>.Count
	{
		get
		{
			return this._items.Length;
		}
	}

	T IReadOnlyList<!0>.this[int index]
	{
		get
		{
			return this._items[index];
		}
	}

	int ICollection<!0>.Count
	{
		get
		{
			return this._items.Length;
		}
	}

	bool ICollection<!0>.IsReadOnly
	{
		get
		{
			return true;
		}
	}

	void ICollection<!0>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<!0>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<!0>.Contains(T item)
	{
		return this._items.Contains(item);
	}

	void ICollection<!0>.CopyTo(T[] array, int arrayIndex)
	{
		this._items.CopyTo(array, arrayIndex);
	}

	bool ICollection<!0>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	T IList<!0>.this[int index]
	{
		get
		{
			return this._items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	int IList<!0>.IndexOf(T item)
	{
		return this._items.IndexOf(item);
	}

	void IList<!0>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<!0>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	[CompilerGenerated]
	private readonly T[] _items;
}
