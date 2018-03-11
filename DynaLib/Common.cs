using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;

namespace Kobdik.Common
{
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

    public interface IDynaCommand
    {
        IDataReader Select();
        IDataReader Detail(int idn);
        IDynaProp[] Action(string cmd);
        string Result { get; }
    }

    public interface IDynaObject : IDisposable
    {
        Dictionary<String, IDynaProp> ParmDict { get; }
        Dictionary<String, IDynaProp> PropDict { get; }
        void ReadPropStream(Stream stream, string cmd);
        void SelectToStream(Stream stream);
        void DetailToStream(Stream stream, int idn);
        void ActionToStream(Stream stream, string cmd);
        string GetInfo(string kind);
    }

}
