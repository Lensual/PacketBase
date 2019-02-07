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

        object GetField(ObjectMetaInfo meta)
        {
            //处理特殊类型
            if (meta.Type == typeof(byte[]))
            {
                byte[] data = new byte[meta.Length];
                Marshal.Copy(RawPtr + meta.Offset, data, 0, meta.Length);
                return data;
            }
            else if (meta.Type.IsEnum)
            {
                meta.Type = Enum.GetUnderlyingType(meta.Type);
            }
            IntPtr objPtr = Marshal.AllocHGlobal(meta.Length);
            CopyMemory(this.RawPtr, meta.Offset, objPtr, 0, meta.Length);
            object obj;
            obj = Marshal.PtrToStructure(objPtr, meta.Type);
            Marshal.FreeHGlobal(objPtr);
            return obj;
        }

        void SetField(ObjectMetaInfo meta, object obj)
        {
            int size = GetStructSize(obj, meta.Type);
            if (size > meta.Length)
                Console.WriteLine("object length bigger than byte array");  //todo 更好的log
            //处理特殊类型
            if (meta.Type == typeof(byte[]))
            {
                Marshal.Copy((byte[])obj, 0, this.RawPtr+meta.Offset, size);
                return;
            }
            else if (meta.Type.IsEnum)
            {
                obj = Convert.ChangeType(obj, Enum.GetUnderlyingType(meta.Type));
            }
            IntPtr objPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, objPtr, false);
            CopyMemory(objPtr, 0, this.RawPtr, meta.Offset, size);
            Marshal.FreeHGlobal(objPtr);
        }

        /// <summary>
        /// 区分字节序复制内存
        /// </summary>
        /// <param name="srcPtr">源指针</param>
        /// <param name="srcOft">源偏移</param>
        /// <param name="dstPtr">目标指针</param>
        /// <param name="dstOft">目标偏移</param>
        /// <param name="Length">复制长度</param>
        static void CopyMemory(IntPtr srcPtr, int srcOfs, IntPtr dstPtr, int dstOfs, int Length)
        {
            for (int i = 0; i < Length; i++)
            {
                if (BitConverter.IsLittleEndian)
                {
                    Marshal.WriteByte(dstPtr, dstOfs + i, Marshal.ReadByte(srcPtr, srcOfs + Length - 1 - i)); //顺写逆读
                }
                else
                {
                    Marshal.WriteByte(dstPtr, dstOfs + i, Marshal.ReadByte(srcPtr, srcOfs + i)); //顺写顺读
                }
            }
        }

        /// <summary>
        /// 改变对象在byte[]中的长度，会清空该对象数据
        /// </summary>
        void changeLength(string name, int newLength)
        {
            ObjectMetaInfo meta = fields[name];
            int oldLength = meta.Length;

            IntPtr newRawPtr = Marshal.AllocHGlobal(newLength);

            //copy bytes
            for (int i = 0; i < meta.Offset + oldLength; i++) //前+自己本身
            {
                Marshal.WriteByte(newRawPtr, i, Marshal.ReadByte(this.RawPtr, i));
            }
            for (int i = meta.Offset + oldLength; i < Length; i++) //后
            {
                Marshal.WriteByte(newRawPtr, i + newLength - oldLength, Marshal.ReadByte(this.RawPtr, i));
            }

            //调整长度
            meta.Length = newLength;
            fields[name] = meta;
            this._length += newLength - oldLength;

            //调整后面成员偏移
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].Offset > meta.Offset)
                {
                    ObjectMetaInfo oldMeta = fields[i];
                    oldMeta.Offset += newLength - oldLength;
                    fields[i] = oldMeta;
                }
            }
        }

        /// <summary>
        /// 返回特殊类型的字节长度
        /// </summary>
        int GetStructSize(object obj, Type type = null)
        {
            if (type == null) type = obj.GetType();
            //处理特殊类型
            if (type.IsEnum)
            {
                return Marshal.SizeOf(Enum.GetUnderlyingType(type));
            }
            else if (type == typeof(byte[]))
            {
                return ((byte[])obj).Length;
            }
            else
            {
                return Marshal.SizeOf(type);
            }
        }

        #region 公开方法

        public PacketBase(int length)
        {
            RawPtr = Marshal.AllocHGlobal(length);
            this._length = length;
        }

        /// <summary>
        /// 包长度
        /// </summary>
        public int Length { get => this._length; }
        int _length;

        /// <summary>
        /// 向数据包结构添加字段
        /// </summary>
        /// <typeparam name="T">字段类型</typeparam>
        /// <param name="name">字段名</param>
        /// <param name="value">给一个初始化值，可以null</param>
        /// <param name="offset">在包结构中的偏移，默认为null在最后字段追加</param>
        /// <param name="length">字段长度，默认为null自动将类型长度作为字段长度</param>
        public void AddField<T>(string name, T value, int? offset = null, int? length = null)
        {
            if (length == null)
            {
                length = GetStructSize(value, typeof(T));
            }
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
                SetField(meta, value);
            }

        }

        /// <summary>
        /// 获取字段存储的对象
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public object Get(string name)
        {
            ObjectMetaInfo meta = fields[name];
            return GetField(meta);
        }

        /// <summary>
        /// 设置字段存储的对象
        /// </summary>
        /// <param name="name">字段名</param>
        /// <param name="obj">对象</param>
        public void Set(string name, object obj)
        {
            ObjectMetaInfo meta = fields[name];
            SetField(meta, obj);
        }

        /// <summary>
        /// 返回或设置网络字节序的byte数组
        /// </summary>
        public byte[] Raw
        {
            get
            {
                byte[] data = new byte[this.Length];
                Marshal.Copy(this.RawPtr, data, 0, this.Length);
                return data;
            }
            set
            {
                Marshal.Copy(value, 0, this.RawPtr, this.Length);
            }
        }

        #endregion

        #region override

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            ObjectMetaInfo meta;
            if (fields.TryGetValue(binder.Name, out meta))
            {
                SetField(meta, value);
                return true;
            }
            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            ObjectMetaInfo meta;
            if (fields.TryGetValue(binder.Name, out meta))
            {
                result = GetField(meta);
                return true;
            }
            result = null;
            return false;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this;
        }

        public override string ToString()
        {
            string[] names = this.GetDynamicMemberNames().ToArray();
            string outstr = "";
            for (int i = 0; i < names.Length; i++)
            {
                object obj = this.Get(names[i]);
                string itemStr;
                //byte[] to hex string
                if (obj.GetType() == typeof(byte[]))
                    itemStr = BitConverter.ToString((byte[])obj);
                else
                    itemStr = obj.ToString();
                outstr += String.Format("{0}: {1}", names[i], itemStr);
                if (i < names.Length - 1)
                {
                    outstr += "\n";
                }
            }
            return outstr;
        }

        #endregion

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
                if (this.RawPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(this.RawPtr);
                    this.RawPtr = IntPtr.Zero;
                }

                // TODO: 将大型字段设置为 null。
                fields = null;

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        ~PacketBase()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            GC.SuppressFinalize(this);
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
