using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;

namespace Kobdik.Common
{
    public delegate void NotifyEvent(String message);

    public interface IPropWriter
    {
        void WriteProp(String propName, String value);
        void WriteProp(String propName, Byte value);
        void WriteProp(String propName, Int16 value);
        void WriteProp(String propName, Int32 value);
        void WriteProp(String propName, DateTime value);
        void WriteProp(String propName, Double value);
    }

    public interface IStreamReader
    {
        void Open(Stream stream);
        bool Read();
        int TokenType();
        object Value();
        void Close();
    }

    public interface IStreamWriter : IPropWriter
    {
        byte GetStreamType();
        void Open(Stream stream);
        void PushArr();
        void PushObj();
        void PushArrProp(string propName);
        void PushObjProp(string propName);
        void Pop();
        void Close();
        string Result { get; }
    }

    public interface IDynaProp
    {
        string GetName();
        DbType GetDbType();
        Type GetPropType();
        int GetSize();
        byte GetFlags();
        int Ordinal { get; set; }
        void ReadProp(IDataRecord record);
        void WriteProp(IDataRecord record, IPropWriter writer);
        void WriteProp(IPropWriter writer);
        Object Value { get; set; }
    }

    public interface IDbQuery : IDisposable
    {
        IDataReader Select(IDynaProp[] parms);
        IDataReader Detail(IDynaProp prop);
        void Action(IDynaProp[] props, string cmd);
        string Result { get; }
    }

    public interface IDynaObject : IDisposable
    {
        Dictionary<String, IDynaProp> ParmDict { get; }
        Dictionary<String, IDynaProp> PropDict { get; }
        void ReadPropStream(Stream stream, string cmd);
        IDataReader Select();
        IDataReader Detail(int idn);
        IDynaProp[] Action(string cmd);
        IStreamWriter StreamWriter { get; set; }
        void SelectToStream(Stream stream);
        void DetailToStream(Stream stream, int idn);
        void ActionToStream(Stream stream, string cmd);
        string Result { get; }
        string GetInfo();
    }

    #region DictDefinitions

    public class QryDef
    {
        public byte qry_id, master, col_def;
        public string qry_name, qry_head;
        public byte qry_flags, col_flags;
        public byte[] groups;
    }

    public class PamDef
    {
        public byte qry_id, pam_id, pam_type;
        public string pam_name, def_val;
        public short pam_size;
    }

    public class ColDef
    {
        public byte qry_id, col_id, col_type, look_qry, look_key, look_res;
        public string col_name, col_head;
        public short col_size;
        public byte col_flags, ext_flags;
    }

    #endregion

}
