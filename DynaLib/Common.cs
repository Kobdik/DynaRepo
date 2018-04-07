using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;

namespace Kobdik.Common
{
    #region DictDefinitions

    public class QryDef
    {
        public string qry_name, qry_head, qry_lord, fld_dict;
        public int qry_mask;
        //public byte[] groups;
    }

    public class FldDef
    {
        public string qry_name, fld_name, fld_head, def_val;
        //string look_qry, look_key, look_res;
        public int fld_type, fld_size, inp_mask, out_mask;
    }

    #endregion

    public static class CmdBit
    {
        public static int Sel = 1, Det = 2, Ins = 4, Upd = 8, C16 = 16, C32 = 32, C64 = 64;

        public static int GetBit(string cmd)
        {
            int cmd_bit = 0;
            switch (cmd)
            {
                case "sel": cmd_bit = Sel; break;
                case "det": cmd_bit = Det; break;
                case "ins": cmd_bit = Ins; break;
                case "upd": cmd_bit = Upd; break;
                case "c16": cmd_bit = C16; break;
                case "c32": cmd_bit = C32; break;
                case "c64": cmd_bit = C64; break;
            }
            return cmd_bit;
        }
    }

    public interface IPropWriter
    {
        void WriteProp(String propName);
        void WriteProp(Byte[] value, int len, int state);
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

    public interface IDynaField
    {
        string GetName();
        DbType GetDbType();
        Type GetPropType();
        int GetSize();
        int GetInpMask();
        int GetOutMask();
        int Ordinal { get; set; }
        object Value { get; set; }
        object GetData(IDataRecord record);
        void WriteProp(IDataRecord record, IPropWriter writer);
        void WriteProp(IPropWriter writer);
    }

    public interface IDataQuery : IDisposable
    {
        IDataReader Select(IEnumerable<IDynaField> fields, CommandBehavior behavior);
        IDataReader Detail(IEnumerable<IDynaField> fields, CommandBehavior behavior);
        IDynaField[] Action(IEnumerable<IDynaField> fields, string cmd);
        int Rows_Affected { get; }
        string Result { get; }
    }

    public interface IDataCommand
    {
        IDataReader Select(CommandBehavior behavior = CommandBehavior.Default);
        IDataReader Detail(CommandBehavior behavior = CommandBehavior.Default);
        void WriteRecord(IDataRecord record, IPropWriter writer);
        //void ReadRecord(IDataRecord record); //DynaReader
        IDynaField[] Action(string cmd);
        int Rows_Affected { get; }
    }

    public interface IDynaRecord : IEnumerable<IDataRecord>, IDisposable
    {
        Dictionary<String, IDynaField> FieldDict { get; }
        void ReadPropStream(Stream stream, string cmd);
        void SelectToStream(Stream stream, CommandBehavior behavior = CommandBehavior.Default);
        void DetailToStream(Stream stream, CommandBehavior behavior = CommandBehavior.Default);
        void ActionToStream(Stream stream, string cmd);
        IStreamReader StreamReader { get; }
        IStreamWriter StreamWriter { get; }
        DynamicObject Ordinal();
        string GetInfo(string kind);
        string Result { get; }
    }

}
