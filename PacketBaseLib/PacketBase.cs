using PacketBaseLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PacketBaseLib
{

    public class ListDict<TKey, TValue> : IEnumerable<TValue>
    {
        List<KeyValuePair<TKey, TValue>> list = new List<KeyValuePair<TKey, TValue>>();
        public TValue this[int index]
        {
            get => list[index].Value;
            set => list[index] = new KeyValuePair<TKey, TValue>(list[index].Key, value);
        }

        public TValue this[TKey key] { get => GetValue(key); set => SetValue(key, value); }

        public TValue[] ToArray()
        {
            TValue[] arr = new TValue[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                arr[i] = this[i];
            }
            return arr;
        }

        TValue GetValue(TKey key)
        {
            for (int i = 0; i < list.Count; i++)
            {
                KeyValuePair<TKey, TValue> kvp = list[i];
                if (key.Equals(kvp.Key))
                    return kvp.Value;
            }
            throw new KeyNotFoundException();
        }
        void SetValue(TKey key, TValue value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                KeyValuePair<TKey, TValue> kvp = list[i];
                if (key.Equals(kvp.Key))
                {
                    list[i] = new KeyValuePair<TKey, TValue>(key, value);
                    return;
                }
            }
            this.Add(key, value);
        }

        public void Add(TKey key, TValue value)
        {
            list.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public TValue Last { get => list.Last().Value; }

        public int Count { get => list.Count; }

        public bool ContainsKey(TKey key)
        {
            foreach (KeyValuePair<TKey, TValue> item in list)
            {
                if (item.Key.Equals(key))
                    return true;
            }
            return false;
        }

        public bool ContainsValue(TValue value)
        {
            foreach (KeyValuePair<TKey, TValue> item in list)
            {
                if (item.Value.Equals(value))
                    return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            foreach (KeyValuePair<TKey, TValue> item in list)
            {
                if (item.Key.Equals(key))
                {
                    value = item.Value;
                    return true;
                }
            }
            value = default(TValue);
            return false;
        }

        #region 枚举接口实现
        public IEnumerator<TKey> GetKeyEnumerator()
        {
            return new KeyEnumerator(list);
        }

        public IEnumerator<TValue> GetValueEnumerator()
        {
            return new ValueEnumerator(list);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return GetValueEnumerator();
        }
        #endregion

        #region 枚举器
        class KeyEnumerator : IEnumerator<TKey>
        {
            IEnumerator<KeyValuePair<TKey, TValue>> listEnum;
            public KeyEnumerator(List<KeyValuePair<TKey, TValue>> list)
            {
                listEnum = list.GetEnumerator();
            }
            TKey IEnumerator<TKey>.Current => listEnum.Current.Key;

            object IEnumerator.Current => listEnum.Current.Key;

            void IDisposable.Dispose()
            {
                listEnum.Dispose();
            }

            bool IEnumerator.MoveNext()
            {
                return listEnum.MoveNext();
            }

            void IEnumerator.Reset()
            {
                listEnum.Reset();
            }
        }
        class ValueEnumerator : IEnumerator<TValue>
        {
            IEnumerator<KeyValuePair<TKey, TValue>> listEnum;
            public ValueEnumerator(List<KeyValuePair<TKey, TValue>> list)
            {
                listEnum = list.GetEnumerator();
            }
            TValue IEnumerator<TValue>.Current => listEnum.Current.Value;

            object IEnumerator.Current => listEnum.Current.Value;

            void IDisposable.Dispose()
            {
                listEnum.Dispose();
            }

            bool IEnumerator.MoveNext()
            {
                return listEnum.MoveNext();
            }

            void IEnumerator.Reset()
            {
                listEnum.Reset();
            }
        }
        #endregion

    }

    public struct ObjectMetaInfo
    {
        public int Offset;
        public int Length;
        public Type Type;
    }

    public class PacketBase : System.Dynamic.DynamicObject, IDisposable, IEnumerable<string>
    {
        IntPtr RawPtr;
        ListDict<string, ObjectMetaInfo> fields = new ListDict<string, ObjectMetaInfo>();
        public PacketBase(int length)
        {
            RawPtr = Marshal.AllocHGlobal(length);
        }

        public void AddField<T>(string name, T value, int? offset = null, int? length = null)
        {
            if (length == null)
                length = Marshal.SizeOf(typeof(T));
            if (offset == null)
            {
                if (fields.Count > 0)
                    offset = fields.Last.Offset + fields.Last.Length;
                else
                    offset = 0;
            }
            ObjectMetaInfo meta = new ObjectMetaInfo()
            {
                Length = (int)length,
                Offset = (int)offset,
                Type = typeof(T)
            };
            fields.Add(name, meta);

            if (value != null)
            {
                SetField(name, value);
            }

        }


        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (fields.ContainsKey(binder.Name))
            {
                SetField(binder.Name, value);
                return true;
            }
            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            ObjectMetaInfo meta;
            if (fields.TryGetValue(binder.Name, out meta))
            {
                result = GetField(binder.Name);
                return true;
            }
            result = null;
            return false;
        }

        //todo getfield setfield 优化多次在fields中查询meta
        object GetField(string name)
        {
            ObjectMetaInfo meta = fields[name];
            IntPtr objPtr = Marshal.AllocHGlobal(meta.Length); //todo 释放
            //字节序
            for (int i = 0; i < meta.Length; i++)
            {
                if (BitConverter.IsLittleEndian)   //todo 抽出过程
                {
                    Marshal.WriteByte(objPtr, i, Marshal.ReadByte(this.RawPtr, meta.Offset + meta.Length -1 - i));
                }
                else
                {
                    Marshal.WriteByte(objPtr, i, Marshal.ReadByte(this.RawPtr, meta.Offset + i));
                }
            }
            return Marshal.PtrToStructure(objPtr, meta.Type);
        }

        void SetField(string name, object obj)
        {
            ObjectMetaInfo meta = fields[name];
            int size = Marshal.SizeOf(obj);
            if (size > meta.Length)
                Console.WriteLine("object length bigger than byte array");  //todo 更好的log
            IntPtr objPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, objPtr, false); //todo 释放
                                                        //字节序
            for (int i = 0; i < meta.Length; i++)
            {
                if (BitConverter.IsLittleEndian)   //todo 抽出过程
                {
                    Marshal.WriteByte(this.RawPtr, meta.Offset + i, Marshal.ReadByte(objPtr, meta.Length - 1 - i));
                }
                else
                {
                    Marshal.WriteByte(this.RawPtr, meta.Offset + i, Marshal.ReadByte(objPtr, i));
                }
            }
        }


        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this;
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~PacketBase() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }


        #endregion

        #region 枚举接口实现
        public IEnumerator<string> GetEnumerator()
        {
            return this.fields.GetKeyEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

    }
}
