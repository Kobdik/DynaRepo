using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Kobdik.Common;

namespace Kobdik.Dynamics
{
    public abstract class DynaField : IDynaField
    {
        protected int fld_size, inp_flags, out_flags;
        protected string fld_name;
        protected DbType fld_dbtype;
        protected Type fld_type;

        public DynaField(string fldName, DbType fldType, int fldSize, int inpFlags, int outFlags)
        {
            fld_name = fldName;
            fld_dbtype = fldType;
            fld_size = fldSize;
            inp_flags = inpFlags;
            out_flags = outFlags;
        }
        public string GetName() { return fld_name; }
        public DbType GetDbType() { return fld_dbtype; }
        public Type GetPropType() { return fld_type; }
        public int GetSize() { return fld_size; }
        public int GetInpMask() { return inp_flags; }
        public int GetOutMask() { return out_flags; }
        public abstract Object Value { get; set; }
        public int Ordinal { get; set; }
        public abstract object GetData(IDataRecord record);
        public abstract void WriteProp(IDataRecord record, IPropWriter writer);
        public abstract void WriteProp(IPropWriter writer);
    }

    public sealed class StringField : DynaField
    {
        private string fld_value;

        public StringField(string name, DbType dbtype, int size, int inpFlags, int outFlags) : base(name, dbtype, size, inpFlags, outFlags)
        {
            fld_type = typeof(string);
        }

        public override object GetData(IDataRecord record)
        {
            return record.GetString(Ordinal);
        }

        public override void WriteProp(IDataRecord record, IPropWriter writer)
        {
            writer.WriteProp(fld_name, record.GetString(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(fld_name, fld_value);
        }

        public override Object Value
        {
            get { return fld_value; }
            set { SetValue(value as string); }
        }

        private void SetValue(string str_val)
        {
            if (str_val.Length > fld_size)
                fld_value = str_val.Substring(0, fld_size);
            else
                fld_value = str_val;
        }
    }

    public sealed class TextField : DynaField
    {
        private string fld_value;
        private byte[] fld_buff;

        public TextField(string name, DbType dbtype, int size, int inpFlags, int outFlags) : base(name, dbtype, size, inpFlags, outFlags)
        {
            fld_type = typeof(string);
            fld_buff = new byte[size];
        }

        public override object GetData(IDataRecord record)
        {
            return record.GetString(Ordinal);
        }

        public override void WriteProp(IDataRecord record, IPropWriter writer)
        {
            writer.WriteProp(fld_name);
            long length, offset = 0;
            //const int start = 1, finish = 2;
            int state = 1;
            do //sequential access
            {
                length = record.GetBytes(Ordinal, offset, fld_buff, 0, fld_size);
                if (length < fld_size) state += 2;
                //write chunk of data
                writer.WriteProp(fld_buff, (int)length, state);
                offset += length;
                state = 0;
            }
            while (length == fld_size);
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(fld_name, fld_value);
        }

        public override Object Value
        {
            get { return fld_value; }
            set { fld_value = value as string; }
        }

    }

    public sealed class ByteField : DynaField
    {
        private byte fld_value;

        public ByteField(string name, DbType dbtype, int size, int inpFlags, int outFlags) : base(name, dbtype, size, inpFlags, outFlags)
        {
            fld_type = typeof(byte);
        }

        public override object GetData(IDataRecord record)
        {
            return record.GetByte(Ordinal);
        }

        public override void WriteProp(IDataRecord record, IPropWriter writer)
        {
            writer.WriteProp(fld_name, record.GetByte(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(fld_name, fld_value);
        }

        public override Object Value
        {
            get { return fld_value; }
            set { fld_value = Convert.ToByte(value); }
        }
    }

    public sealed class Int16Field : DynaField
    {
        private Int16 fld_value;

        public Int16Field(string name, DbType dbtype, int size, int inpFlags, int outFlags) : base(name, dbtype, size, inpFlags, outFlags)
        {
            fld_type = typeof(Int16);
        }

        public override object GetData(IDataRecord record)
        {
            return record.GetInt16(Ordinal);
        }

        public override void WriteProp(IDataRecord record, IPropWriter writer)
        {
            writer.WriteProp(fld_name, record.GetInt16(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(fld_name, fld_value);
        }

        public override Object Value
        {
            get { return fld_value; }
            set { fld_value = Convert.ToInt16(value); }
        }
    }

    public sealed class Int32Field : DynaField
    {
        private Int32 fld_value;

        public Int32Field(string name, DbType dbtype, int size, int inpFlags, int outFlags) : base(name, dbtype, size, inpFlags, outFlags)
        {
            fld_type = typeof(Int32);
        }

        public override object GetData(IDataRecord record)
        {
            return record.GetInt32(Ordinal);
        }

        public override void WriteProp(IDataRecord reader, IPropWriter writer)
        {
            writer.WriteProp(fld_name, reader.GetInt32(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(fld_name, fld_value);
        }

        public override Object Value
        {
            get { return fld_value; }
            set { fld_value = Convert.ToInt32(value); }
        }
    }

    public sealed class DateField : DynaField
    {
        private DateTime fld_value;

        public DateField(string name, DbType dbtype, int size, int inpFlags, int outFlags) : base(name, dbtype, size, inpFlags, outFlags)
        {
            fld_type = typeof(DateTime);
        }

        public override object GetData(IDataRecord record)
        {
            return record.GetDateTime(Ordinal);
        }

        public override void WriteProp(IDataRecord reader, IPropWriter writer)
        {
            writer.WriteProp(fld_name, reader.GetDateTime(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(fld_name, fld_value);
        }

        public override Object Value
        {
            get { return fld_value; }
            set { fld_value = Convert.ToDateTime(value); }
        }
    }

    public sealed class DoubleField : DynaField
    {
        private Double fld_value;

        public DoubleField(string name, DbType dbtype, int size, int inpFlags, int outFlags) : base(name, dbtype, size, inpFlags, outFlags)
        {
            fld_type = typeof(double);
        }

        public override object GetData(IDataRecord record)
        {
            return record.GetDouble(Ordinal);
        }

        public override void WriteProp(IDataRecord reader, IPropWriter writer)
        {
            writer.WriteProp(fld_name, reader.GetDouble(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(fld_name, fld_value);
        }

        public override Object Value
        {
            get { return fld_value; }
            set { fld_value = Convert.ToDouble(value); }
        }
    }

    public class DataQuery : IDataQuery
    {
        private IDbConnection db_conn;
        private IDataReader db_reader;
        private string qry_name;

        public DataQuery(IDbConnection dbConn, string qryName)
        {
            db_conn = dbConn; qry_name = qryName;
        }

        public IDataReader Select(IEnumerable<IDynaField> fields, CommandBehavior behavior)
        {
            string proName = "sel_" + qry_name;
            IDbCommand selComm = db_conn.CreateCommand();
            selComm.CommandType = CommandType.StoredProcedure;
            selComm.CommandText = proName;
            //fill select parameters
            foreach (IDynaField field in fields.Where(fld => (fld.GetInpMask() & CmdBit.Sel) > 0))
            {
                IDbDataParameter param = selComm.CreateParameter();
                param.ParameterName = String.Format("@{0}", field.GetName());
                param.DbType = field.GetDbType();
                param.Size = field.GetSize();
                //converted by db engine
                param.Value = field.Value;
                selComm.Parameters.Add(param);
            }
            db_reader = null;
            try
            {
                db_conn.Open();
                db_reader = selComm.ExecuteReader(behavior);
                Result = "Ok";
            }
            catch (Exception ex)
            {
                Result = ex.Message;
            }
            //send reader
            return db_reader;
        }

        public IDataReader Detail(IEnumerable<IDynaField> fields, CommandBehavior behavior)
        {
            String proName = "det_" + qry_name;
            IDbCommand detComm = db_conn.CreateCommand();
            detComm.CommandType = CommandType.StoredProcedure;
            detComm.CommandText = proName;
            //fill detail parameters
            foreach (IDynaField field in fields.Where(fld => (fld.GetInpMask() & CmdBit.Det) > 0))
            {
                IDbDataParameter param = detComm.CreateParameter();
                param.ParameterName = String.Format("@{0}", field.GetName());
                param.DbType = field.GetDbType();
                param.Size = field.GetSize();
                //converted by db engine
                param.Value = field.Value;
                detComm.Parameters.Add(param);
            }
            db_reader = null;
            try
            {
                db_conn.Open();
                db_reader = detComm.ExecuteReader(behavior);
                Result = "Ok";
            }
            catch (Exception ex)
            {
                Result = ex.Message;
            }
            //send reader
            return db_reader;
        }

        public IDynaField[] Action(IEnumerable<IDynaField> fields, string cmd)
        {
            int cmd_bit = CmdBit.GetBit(cmd);
            //cmd_bit: 4 - ins, 8 - upd, 16 - c16, 32 - c32, 64 - c64
            IDbCommand actComm = db_conn.CreateCommand();
            actComm.CommandType = CommandType.StoredProcedure;
            actComm.CommandText = String.Format("{0}_{1}", cmd, qry_name);
            IDynaField[] inp_fields = fields.Where(fld => (fld.GetInpMask() & cmd_bit) > 0).ToArray<IDynaField>();
            IDynaField[] out_fields = inp_fields.Where(fld => (fld.GetOutMask() & cmd_bit) > 0).ToArray<IDynaField>();
            //fill input parameters
            foreach (IDynaField field in inp_fields)
            {
                IDbDataParameter param = actComm.CreateParameter();
                param.ParameterName = String.Format("@{0}", field.GetName());
                param.DbType = field.GetDbType();
                param.Size = field.GetSize();
                param.Value = field.Value;
                if ((field.GetOutMask() & cmd_bit) > 0) param.Direction = ParameterDirection.InputOutput;
                actComm.Parameters.Add(param);
            }
            try
            {
                db_conn.Open();
                Rows_Affected = actComm.ExecuteNonQuery();
                if (Rows_Affected < 0)
                {
                    if (actComm.Parameters.Contains("@Message"))
                    {
                        IDataParameter param = actComm.Parameters["@Message"] as IDataParameter;
                        Result = param.Value as string;
                    }
                    else
                        Result = "Не удалось выполнить команду!";
                    //alarm with error message
                    //throw new Exception(Result);
                }
                else
                    Result = "Ok";
                //obtain result from output fields
                foreach (IDynaField field in out_fields)
                {
                    string paramName = String.Format("@{0}", field.GetName());
                    IDataParameter param = actComm.Parameters[paramName] as IDataParameter;
                    field.Value = param.Value;                    
                }
            }
            catch (Exception ex)
            {
                Result = ex.Message;
            }
            finally
            {
                db_conn.Close();
            }
            return out_fields;
        }

        public int Rows_Affected
        {
            get; set;
        }

        public string Result
        {
            get; set;
        }

        public void Dispose()
        {
            db_reader?.Close();
            db_conn?.Close();
        }
    }

    public class DynamicOrdinal : DynamicObject
    {
        private IDataReader _dbreader;

        public DynamicOrdinal(IDataReader dbreader)
        {
            _dbreader = dbreader;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            int i_ord = -1;
            try
            {
                i_ord = _dbreader.GetOrdinal(binder.Name);
            }
            catch (Exception)
            {
            }
            finally
            {
                result = i_ord;
            }
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return false;
        }
    }

    public class DynaRecord : IDynaRecord, IDataCommand, IEnumerator<IDataRecord>, IEnumerable<IDataRecord>
    {
        #region fields
        private string qry_name;
        private object lockObj;
        private IDataRecord current;
        private IDataReader _reader;
        private DynamicObject ordinal;
        #endregion fields

        #region properties
        public string QryName { get { return qry_name; } }
        public Dictionary<String, IDynaField> FieldDict { get; set; }
        public List<IDynaField> ReadList { get; set; }
        public List<DynaRecord> SlaveList { get; set; }
        public IStreamReader StreamReader { get; set; }
        public IStreamWriter StreamWriter { get; set; }
        public IDataQuery Query { get; set; }
        public String Result { get; set; }
        #endregion

        public DynaRecord(QryDef qryDef)
        {
            qry_name = qryDef.qry_name;
            //dynamic properties
            FieldDict = new Dictionary<String, IDynaField>(16);
            //ordinal properties
            ReadList = new List<IDynaField>(16);
            lockObj = new Object();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            //throw new NotImplementedException();
            return ((IEnumerable<IDataRecord>)this).GetEnumerator();
        }

        public IEnumerator<IDataRecord> GetEnumerator()
        {
            if (_reader == null || _reader.IsClosed) Reset();
            return this;
        }

        object IEnumerator.Current => current;

        public IDataRecord Current => current;

        public bool MoveNext()
        {
            if (_reader == null) return false;
            bool hasNext = _reader.Read();
            if (!hasNext) _reader.Close();
            //move ahead and update current
            return hasNext;
        }

        public void Reset()
        {
            _reader = Query.Select(FieldDict.Values, CommandBehavior.SequentialAccess);
            current = _reader;
            if (_reader != null)
                ordinal = new DynamicOrdinal(_reader);
            else
                ordinal = null;
        }

        public DynamicObject Ordinal()
        {
            Reset();
            return ordinal;
        }

        public void CreateField(FldDef fldDef)
        {
            IDynaField field = null;
            switch (fldDef.fld_type)
            {
                case 167: //varchar
                    field = new StringField(fldDef.fld_name, DbType.String, fldDef.fld_size, fldDef.inp_mask, fldDef.out_mask);
                    break;
                case 175: //char[]->varchar
                    field = new StringField(fldDef.fld_name, DbType.StringFixedLength, fldDef.fld_size, fldDef.inp_mask, fldDef.out_mask);
                    break;
                case 35: //text
                    field = new TextField(fldDef.fld_name, DbType.AnsiString, fldDef.fld_size, fldDef.inp_mask, fldDef.out_mask);
                    break;
                case 48: //byte
                    field = new ByteField(fldDef.fld_name, DbType.Byte, fldDef.fld_size, fldDef.inp_mask, fldDef.out_mask);
                    break;
                case 52: //int16
                    field = new Int16Field(fldDef.fld_name, DbType.Int16, fldDef.fld_size, fldDef.inp_mask, fldDef.out_mask);
                    break;
                case 56: //int32
                    field = new Int32Field(fldDef.fld_name, DbType.Int32, fldDef.fld_size, fldDef.inp_mask, fldDef.out_mask);
                    break;
                case 40: //date
                    field = new DateField(fldDef.fld_name, DbType.Date, fldDef.fld_size, fldDef.inp_mask, fldDef.out_mask);
                    break;
                case 61: //datetime
                    field = new DateField(fldDef.fld_name, DbType.DateTime, fldDef.fld_size, fldDef.inp_mask, fldDef.out_mask);
                    break;
                case 62: //double
                    field = new DoubleField(fldDef.fld_name, DbType.Double, fldDef.fld_size, fldDef.inp_mask, fldDef.out_mask);
                    break;
            }
            if (field != null)
            {
                field.Value = fldDef.def_val;
                FieldDict.Add(fldDef.fld_name, field);
            }
        }

        public void ReadPropStream(Stream stream, string cmd)
        {
            string key;
            int i_type;
            // read parameters
            StreamReader.Open(stream);
            while (StreamReader.Read())
            {
                i_type = StreamReader.TokenType();
                if (i_type == 4) // PropName
                {
                    key = StreamReader.Value() as string;
                    StreamReader.Read(); // read Value
                    if (FieldDict.ContainsKey(key))
                        FieldDict[key].Value = StreamReader.Value();
                }
            }
            StreamReader.Close();
        }

        private void ReadOrdinals(IDataReader reader, int cmd_bit)
        {
            string key;
            IDynaField field;
            ReadList.Clear();
            foreach (var p in FieldDict.Values) p.Ordinal = -1;
            int i_ord, i_count = reader.FieldCount;
            for (i_ord = 0; i_ord < i_count; i_ord++)
            {
                key = reader.GetName(i_ord);
                if (FieldDict.Keys.Contains<string>(key))
                {
                    field = FieldDict[key];
                    //read 1 - for select
                    //read 2 - for detail
                    if ((field.GetOutMask() & cmd_bit) > 0)
                    {
                        field.Ordinal = i_ord;
                        ReadList.Add(field);
                    }
                }
            }
        }

        public IDataReader Select(CommandBehavior behavior)
        {
            IDataReader result = Query.Select(FieldDict.Values, behavior);
            if (result != null) ReadOrdinals(result, CmdBit.Sel);
            return result;
        }

        public IDataReader Detail(CommandBehavior behavior)
        {
            IDataReader result = Query.Detail(FieldDict.Values, behavior);
            if (result != null) ReadOrdinals(result, CmdBit.Det);
            return result;
        }

        public IDynaField[] Action(string cmd)
        {
            var out_fields = Query.Action(FieldDict.Values, cmd);
            Result = Query.Result;
            //fields with returned values
            return out_fields;
        }

        public void WriteRecord(IDataRecord record, IPropWriter writer)
        {
            foreach (var field in ReadList)
                field.WriteProp(record, writer);
        }

        public void SelectToStream(Stream stream, CommandBehavior behavior)
        {
            lock (lockObj)
            {
                IDataReader selReader = null;
                try
                {
                    StreamWriter.Open(stream);
                    StreamWriter.PushObj();
                    StreamWriter.PushArrProp("selected");
                    //DateTime fst = DateTime.Now;
                    selReader = Select(behavior);
                    if (selReader != null)
                    {
                        while (selReader.Read())
                        {
                            //if (token.IsCancellationRequested) break;
                            StreamWriter.PushObj();
                            WriteRecord(selReader, StreamWriter);
                            StreamWriter.Pop();
                        }
                        selReader.Close();
                    };
                    //DateTime lst = DateTime.Now;
                    //TimeSpan ts = lst - fst;
                    StreamWriter.Pop();
                    StreamWriter.WriteProp("message", Query.Result);
                    //StreamWriter.WriteProp("sel_time", lst.ToShortTimeString());
                    //StreamWriter.WriteProp("time_ms", ts.Milliseconds);
                    StreamWriter.Pop();
                    Result = Query.Result;
                }
                catch (Exception ex)
                {
                    Result = ex.Message;
                }
                finally
                {
                    Query.Dispose();
                    //ошибки могут возникать только в Query
                    StreamWriter?.Close();
                }
            }
        }

        public void DetailToStream(Stream stream, CommandBehavior behavior)
        {
            lock (lockObj)
            {
                IDataReader detReader = null;
                IDataReader selReader = null;
                try
                {
                    StreamWriter.Open(stream);
                    StreamWriter.PushObj();
                    detReader = Detail(behavior);
                    if (detReader != null)
                    {
                        StreamWriter.PushObjProp("det_row");
                        if (detReader.Read())
                            WriteRecord(detReader, StreamWriter);
                        StreamWriter.Pop();
                        detReader.Close();
                    };
                    if (Query.Result != "Ok") throw new Exception(Query.Result);
                    //write slaves to stream
                    StreamWriter.PushObjProp("slaves");
                    foreach (var slave in SlaveList)
                    {
                        StreamWriter.PushArrProp(slave.qry_name);
                        //qry_name is master column name
                        slave.FieldDict[qry_name].Value = FieldDict["Idn"].Value;
                        selReader = slave.Select(CommandBehavior.Default);
                        if (selReader != null)
                        {
                            while (selReader.Read())
                            {
                                StreamWriter.PushObj();
                                slave.WriteRecord(selReader, StreamWriter);
                                StreamWriter.Pop();
                            }
                            selReader.Close();
                        };
                        StreamWriter.Pop();
                    }
                    StreamWriter.Pop();
                    StreamWriter.WriteProp("message", Query.Result);
                    StreamWriter.WriteProp("det_time", DateTime.Now.ToShortTimeString());
                    StreamWriter.Pop();
                    Result = Query.Result;
                }
                catch (Exception ex)
                {
                    Result = ex.Message;
                }
                finally
                {
                    Query.Dispose();
                    StreamWriter?.Close();
                }
            }
        }

        public void ActionToStream(Stream stream, string cmd)
        {
            //IStreamResultWriter resultWriter = new JsonStreamResult();
            lock (lockObj)
            {
                try
                {
                    //stream can be null
                    StreamWriter.Open(stream);
                    StreamWriter.PushObj();
                    StreamWriter.WriteProp("cmd", cmd);
                    //read out fields
                    var outFields = Action(cmd);
                    if (Query.Result != "Ok") throw new Exception(Query.Result);
                    //write output parameters
                    StreamWriter.PushObjProp("out");
                    foreach (IDynaField field in outFields)
                        field.WriteProp(StreamWriter);
                    StreamWriter.Pop();
                    StreamWriter.WriteProp("message", Query.Result);
                    StreamWriter.WriteProp("cmd_time", DateTime.Now.ToShortTimeString());
                    StreamWriter.Pop();
                    Result = Query.Result;
                }
                catch (Exception ex)
                {
                    Result = ex.Message;
                }
                finally
                {
                    Query.Dispose();
                    StreamWriter?.Close();
                }
            }
        }

        public int Rows_Affected
        {
            get => Query.Rows_Affected;
        }

        private string GetFieldsInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Query Name: {0}\n", qry_name);
            foreach (var pair in FieldDict)
            {
                IDynaField field = pair.Value;
                sb.AppendFormat("Field: {0}, Value: {1}\n", pair.Key, field.Value);
            }
            sb.AppendFormat("Result: {0}\n", Result);
            return sb.ToString();
        }

        private string GetFldDef(IDynaField field)
        {
            string result = "";
            DbType dbType = field.GetDbType();
            switch (dbType)
            {
                case DbType.String: //varchar
                    result = String.Format("\t{0} varchar({1}) NOT NULL, ", field.GetName(), field.GetSize());
                    break;
                case DbType.StringFixedLength: //char
                    result = String.Format("\t{0} char({1}) NOT NULL, ", field.GetName(), field.GetSize());
                    break;
                case DbType.Byte: //byte
                    result = String.Format("\t{0} tinyint NOT NULL, ", field.GetName());
                    break;
                case DbType.Int16: //int16
                    result = String.Format("\t{0} smallint {1} NOT NULL, ", field.GetName(), (field.GetOutMask() & 128) > 0 ? "IDENTITY(1,1)" : "");
                    break;
                case DbType.Int32: //int32          
                    result = String.Format("\t{0} int {1} NOT NULL, ", field.GetName(), (field.GetOutMask() & 128) > 0 ? "IDENTITY(1,1)" : "");
                    break;
                case DbType.Double: //double
                    result = String.Format("\t{0} float NOT NULL, ", field.GetName());
                    break;
                case DbType.Date: //date
                    result = String.Format("\t{0} date NOT NULL, ", field.GetName());
                    break;
                case DbType.DateTime: //datetime
                    result = String.Format("\t{0} datetime NOT NULL, ", field.GetName());
                    break;
            }
            return result;
        }

        private string GetVarDef(IDynaField field, int cmd_bit)
        {
            string result = "";
            string str_out = (field.GetOutMask() & cmd_bit) > 0 ? " out" : "";
            switch (field.GetDbType())
            {
                case DbType.String: //varchar
                    result = String.Format("@{0} varchar({1}){2}, ", field.GetName(), field.GetSize(), str_out);
                    break;
                case DbType.StringFixedLength: //char
                    result = String.Format("@{0} char({1}){2}, ", field.GetName(), field.GetSize(), str_out);
                    break;
                case DbType.Byte: //byte
                    result = String.Format("@{0} tinyint{1}, ", field.GetName(), str_out);
                    break;
                case DbType.Int16: //int16
                    result = String.Format("@{0} smallint{1}, ", field.GetName(), str_out);
                    break;
                case DbType.Int32: //int32          
                    result = String.Format("@{0} int{1}, ", field.GetName(), str_out);
                    break;
                case DbType.Double: //double
                    result = String.Format("@{0} float{1}, ", field.GetName(), str_out);
                    break;
                case DbType.Date: //date
                    result = String.Format("@{0} date{1}, ", field.GetName(), str_out);
                    break;
                case DbType.DateTime: //datetime
                    result = String.Format("@{0} datetime{1}, ", field.GetName(), str_out);
                    break;
            }
            return result;
        }

        private string GetCreateInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("CREATE TABLE dbo.T_{0}(\n", qry_name);
            //выбрать только поля с установленными флагами
            var fields = FieldDict.Values.Where(p => p.GetOutMask() > 0);
            foreach (var field in fields)
                sb.AppendLine(GetFldDef(field));
            //убрать запятую в конце
            sb[sb.Length - 2] = ' ';
            sb[sb.Length - 1] = '\n';
            sb.AppendFormat(" CONSTRAINT [PK_T_{0}] PRIMARY KEY CLUSTERED ( [Idn] ASC )\n", qry_name);
            sb.AppendLine(") ON [PRIMARY]");
            return sb.ToString();
        }

        private string GetSelectInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("CREATE PROC dbo.sel_{0}\n", qry_name);
            var parms = FieldDict.Values.Where(fld => (fld.GetInpMask() & 1) > 0).ToArray();
            if (parms.Count() > 0)
            {
                foreach (var parm in parms)
                    sb.Append(GetVarDef(parm, 1));
                //убрать запятую в конце
                sb[sb.Length - 2] = ' ';
                sb[sb.Length - 1] = '\n';
            }
            sb.AppendLine("AS");
            sb.Append("SELECT ");
            //выбрать только sel поля
            var fields = FieldDict.Values.Where(fld => (fld.GetOutMask() & 1) > 0);
            foreach (var field in fields)
                sb.AppendFormat("{0}, ", field.GetName());
            //убрать запятую в конце 
            sb[sb.Length - 2] = ' ';
            sb[sb.Length - 1] = '\n';
            sb.AppendFormat("FROM dbo.T_{0}\n", qry_name);
            if (parms.Count() > 0)
            {
                //добавить закомментированными
                sb.Append("--WHERE ");
                foreach (var parm in parms)
                    sb.AppendFormat("{0}=@{0}, ", parm.GetName());
                //убрать запятую в конце
                sb[sb.Length - 2] = ' ';
                sb[sb.Length - 1] = '\n';
            }
            sb.AppendLine("RETURN 0;");
            return sb.ToString();
        }

        private string GetDetailInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("CREATE PROC dbo.det_{0}\n", qry_name);
            var parms = FieldDict.Values.Where(fld => (fld.GetInpMask() & 2) > 0).ToArray();
            if (parms.Count() > 0)
            {
                foreach (var parm in parms)
                    sb.Append(GetVarDef(parm, 1));
                //убрать запятую в конце
                sb[sb.Length - 2] = ' ';
                sb[sb.Length - 1] = '\n';
            }
            sb.AppendLine("AS");
            sb.Append("SELECT ");
            //выбрать только det поля
            var fields = FieldDict.Values.Where(fld => (fld.GetOutMask() & 2) > 0);
            foreach (var field in fields)
                sb.AppendFormat("{0}, ", field.GetName());
            //убрать запятую в конце 
            sb[sb.Length - 2] = ' ';
            sb[sb.Length - 1] = '\n';
            sb.AppendFormat("FROM dbo.T_{0}\n", qry_name);
            if (parms.Count() > 0)
            {
                //добавить закомментированными
                sb.Append("--WHERE ");
                foreach (var parm in parms)
                    sb.AppendFormat("{0}=@{0}, ", parm.GetName());
                //убрать запятую в конце
                sb[sb.Length - 2] = ' ';
                sb[sb.Length - 1] = '\n';
            }
            sb.AppendLine("RETURN 0;");
            return sb.ToString();
        }

        private string GetInsertInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("CREATE PROC dbo.ins_{0}\n", qry_name);
            var parms = FieldDict.Values.Where(fld => (fld.GetInpMask() & 4) > 0).ToArray();
            foreach (var parm in parms)
                sb.Append(GetVarDef(parm, 4));
            //убрать запятую в конце
            sb[sb.Length - 2] = ' ';
            sb[sb.Length - 1] = '\n';
            sb.AppendLine("AS");
            sb.AppendFormat("INSERT INTO dbo.T_{0} (", qry_name);
            foreach (var parm in parms.Skip(1))
                sb.AppendFormat("{0}, ", parm.GetName());
            //убрать запятую в конце
            sb[sb.Length - 2] = ')';
            sb.Append("\nVALUES (");
            foreach (var parm in parms.Skip(1))
                sb.AppendFormat("@{0}, ", parm.GetName());
            //убрать запятую в конце 
            sb[sb.Length - 2] = ')';
            sb[sb.Length - 1] = '\n';
            string pt = (FieldDict["Idn"].GetDbType() == DbType.Int16) ? "smallint" : "int";
            sb.AppendFormat("SET @Idn=CAST(IDENT_CURRENT('dbo.T_{0}') AS {1})\n", qry_name, pt);
            sb.AppendLine("RETURN 0;");
            return sb.ToString();
        }

        private string GetUpdateInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("CREATE PROC dbo.upd_{0}\n", qry_name);
            var parms = FieldDict.Values.Where(fld => (fld.GetInpMask() & 8) > 0).ToArray();
            foreach (var parm in parms)
                sb.Append(GetVarDef(parm, 8));
            //убрать запятую в конце
            sb[sb.Length - 2] = ' ';
            sb[sb.Length - 1] = '\n';
            sb.AppendLine("AS");
            sb.AppendFormat("UPDATE dbo.T_{0} SET\n", qry_name);
            foreach (var prop in parms.Skip(1))
                sb.AppendFormat("{0}=@{0}, ", prop.GetName());
            //убрать запятую в конце 
            sb[sb.Length - 2] = ' ';
            sb[sb.Length - 1] = '\n';
            sb.AppendLine("WHERE Idn=@Idn");
            sb.AppendLine("RETURN 0;");
            return sb.ToString();
        }

        public string GetInfo(string kind)
        {
            string result = "";
            switch (kind)
            {
                case "fields": result = GetFieldsInfo(); break;
                case "create": result = GetCreateInfo(); break;
                case "select": result = GetSelectInfo(); break;
                case "detail": result = GetDetailInfo(); break;
                case "insert": result = GetInsertInfo(); break;
                case "update": result = GetUpdateInfo(); break;
            }
            return result;
        }

        public void Dispose()
        {
            Query.Dispose();
        }

    }

}

/*
        public void ReadRecord(IDataRecord record, Dictionary<int, IDynaProp> pairs)
        {
            foreach (var pair in pairs)
                pair.Value.ReadProp(record, pair.Key);
        }

        public void WriteRecord(IDataRecord record, Dictionary<int, IDynaProp> pairs, IPropWriter writer)
        {
            foreach (var pair in pairs)
                pair.Value.WriteProp(record, pair.Key, writer);
        }

*/
