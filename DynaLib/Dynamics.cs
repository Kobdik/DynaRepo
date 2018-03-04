using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Kobdik.Common;
using Kobdik.DataModule;

namespace Kobdik.Dynamics
{
    public abstract class DynaProp : IDynaProp
    {
        private byte _flags;
        private string _name;
        private DbType _dbtype;
        protected Type _type;
        protected short _size;

        public DynaProp(string name, DbType type, short size, byte flag)
        {
            _name = name;
            _dbtype = type;
            _size = size;
            _flags = flag;
        }
        public string GetName() { return _name; }
        public DbType GetDbType() { return _dbtype; }
        public Type GetPropType() { return _type; }
        public int GetSize() { return _size; }
        public byte GetFlags() { return _flags; }
        public abstract Object Value { get; set; }
        public int Ordinal { get; set; }
        public abstract void ReadProp(IDataRecord record);
        public abstract void WriteProp(IDataRecord record, IPropWriter writer);
        public abstract void WriteProp(IPropWriter writer);
    }

    public sealed class StringProp : DynaProp
    {
        private string _value;

        public StringProp(string name, DbType dbtype, short size, byte flag) : base(name, dbtype, size, flag) { _type = typeof(string); }

        public override void ReadProp(IDataRecord record)
        {
            SetValue(record.GetString(Ordinal));
        }

        public override void WriteProp(IDataRecord record, IPropWriter writer)
        {
            writer.WriteProp(GetName(), record.GetString(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(GetName(), _value);
        }

        public override Object Value
        {
            get { return _value; }
            set { SetValue(value as string); }
        }

        private void SetValue(string str_val)
        {
            if (str_val.Length > _size)
                _value = str_val.Substring(0, _size);
            else
                _value = str_val;

        }
    }

    public sealed class ByteProp : DynaProp
    {
        private byte _value;

        public ByteProp(string name, DbType type, short size, byte flag) : base(name, type, size, flag) { _type = typeof(byte); }

        public override void ReadProp(IDataRecord record) { _value = record.GetByte(Ordinal); }

        public override void WriteProp(IDataRecord record, IPropWriter writer)
        {
            writer.WriteProp(GetName(), record.GetByte(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(GetName(), _value);
        }

        public override Object Value
        {
            get { return _value; }
            set { _value = Convert.ToByte(value); }
        }
    }

    public sealed class Int16Prop : DynaProp
    {
        private Int16 _value;

        public Int16Prop(string name, DbType type, short size, byte flag) : base(name, type, size, flag) { _type = typeof(Int16); }

        public override void ReadProp(IDataRecord record) { _value = record.GetInt16(Ordinal); }

        public override void WriteProp(IDataRecord record, IPropWriter writer)
        {
            writer.WriteProp(GetName(), record.GetInt16(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(GetName(), _value);
        }

        public override Object Value
        {
            get { return _value; }
            set { _value = Convert.ToInt16(value); }
        }
    }

    public sealed class Int32Prop : DynaProp
    {
        private Int32 _value;

        public Int32Prop(string name, DbType type, short size, byte flag) : base(name, type, size, flag) { _type = typeof(Int32); }

        public override void ReadProp(IDataRecord record) { _value = record.GetInt32(Ordinal); }

        public override void WriteProp(IDataRecord reader, IPropWriter writer)
        {
            writer.WriteProp(GetName(), reader.GetInt32(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(GetName(), _value);
        }

        public override Object Value
        {
            get { return _value; }
            set { _value = Convert.ToInt32(value); }
        }
    }

    public sealed class DateProp : DynaProp
    {
        private DateTime _value;

        public DateProp(string name, DbType type, short size, byte flag) : base(name, type, size, flag) { _type = typeof(DateTime); }

        public override void ReadProp(IDataRecord record) { _value = record.GetDateTime(Ordinal); }

        public override void WriteProp(IDataRecord reader, IPropWriter writer)
        {
            writer.WriteProp(GetName(), reader.GetDateTime(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(GetName(), _value);
        }

        public override Object Value
        {
            get { return _value; }
            set { _value = Convert.ToDateTime(value); }
        }
    }

    public sealed class DoubleProp : DynaProp
    {
        private Double _value;

        public DoubleProp(string name, DbType type, short size, byte flag) : base(name, type, size, flag) { _type = typeof(double); }

        public override void ReadProp(IDataRecord record) { _value = record.GetDouble(Ordinal); }

        public override void WriteProp(IDataRecord reader, IPropWriter writer)
        {
            writer.WriteProp(GetName(), reader.GetDouble(Ordinal));
        }

        public override void WriteProp(IPropWriter writer)
        {
            writer.WriteProp(GetName(), _value);
        }

        public override Object Value
        {
            get { return _value; }
            set { _value = Convert.ToDouble(value); }
        }
    }

    public class DbQuery : IDbQuery
    {
        private IDbConnection db_conn;
        private IDataReader db_reader;
        private string qry_name;

        public DbQuery(IDbConnection dbConn, string qryName)
        {
            db_conn = dbConn; qry_name = qryName;
        }

        public IDataReader Select(IDynaProp[] parms)
        {
            string proName = "sel_" + qry_name;
            IDbCommand selComm = db_conn.CreateCommand();
            selComm.CommandType = CommandType.StoredProcedure;
            selComm.CommandText = proName;
            //fill select parameters
            foreach (IDynaProp parm in parms)
            {
                IDbDataParameter param = selComm.CreateParameter();
                param.ParameterName = String.Format("@{0}", parm.GetName());
                param.DbType = parm.GetDbType();
                param.Size = parm.GetSize();
                //converted by db engine
                param.Value = parm.Value;
                selComm.Parameters.Add(param);
            }
            db_reader = null;
            try
            {
                db_conn.Open();
                db_reader = selComm.ExecuteReader();
                Result = "Ok";
            }
            catch (Exception ex)
            {
                Result = ex.Message;
            }
            //send reader
            return db_reader;
        }

        public IDataReader Detail(IDynaProp prop)
        {
            String proName = "det_" + qry_name;
            IDbCommand detComm = db_conn.CreateCommand();
            detComm.CommandType = CommandType.StoredProcedure;
            detComm.CommandText = proName;
            //detail parameters
            IDbDataParameter param = detComm.CreateParameter();
            param.ParameterName = String.Format("@{0}", prop.GetName());
            param.DbType = prop.GetDbType();
            param.Size = prop.GetSize();
            param.Value = prop.Value;
            //param.Size = 4;
            detComm.Parameters.Add(param);
            db_reader = null;
            try
            {
                db_conn.Open();
                db_reader = detComm.ExecuteReader();
                Result = "Ok";
            }
            catch (Exception ex)
            {
                Result = ex.Message;
            }
            //send reader
            return db_reader;
        }

        public void Action(IDynaProp[] props, string cmd)
        {
            IDbCommand actComm = db_conn.CreateCommand();
            actComm.CommandType = CommandType.StoredProcedure;
            actComm.CommandText = String.Format("{0}_{1}", cmd, qry_name);
            //fill update parameters
            foreach (IDynaProp prop in props)
            {
                IDbDataParameter param = actComm.CreateParameter();
                param.ParameterName = String.Format("@{0}", prop.GetName());
                param.DbType = prop.GetDbType();
                param.Size = prop.GetSize();
                param.Value = prop.Value;
                if ((prop.GetFlags() & 8) > 0) param.Direction = ParameterDirection.InputOutput;
                actComm.Parameters.Add(param);
            }
            try
            {
                db_conn.Open();
                if (actComm.ExecuteNonQuery() == 0)
                {
                    if (actComm.Parameters.Contains("@Cause"))
                    {
                        IDataParameter param = actComm.Parameters["@Cause"] as IDataParameter;
                        Result = param.Value as string;
                    }
                    else
                        Result = "Не удалось выполнить команду!";
                    //alarm with error message
                    throw new Exception(Result);
                }
                //obtain result from prop values
                foreach (IDynaProp prop in props)
                {
                    if ((prop.GetFlags() & 8) > 0)
                    {
                        string parameterName = String.Format("@{0}", prop.GetName());
                        IDataParameter param = actComm.Parameters[parameterName] as IDataParameter;
                        prop.Value = param.Value;
                    }
                }
                Result = "Ok";
            }
            catch (Exception ex)
            {
                Result = ex.Message;
            }
            finally
            {
                db_conn.Close();
            }
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

    public class DynaObject : IDynaObject, IDynaCommand
    {
        #region fields
        private string qry_name;
        private byte qry_id, src_id, col_flags;
        private object lockObj;
        #endregion fields

        #region properties
        public byte QryId { get { return qry_id; } }
        //public string QryName { get { return qry_name; } }
        public Dictionary<String, IDynaProp> PropDict { get; set; }
        public Dictionary<String, IDynaProp> ParmDict { get; set; }
        public List<IDynaProp> ReadList { get; set; }
        public List<DynaObject> SlaveList { get; set; }
        public IStreamReader StreamReader { get; set; }
        public IStreamWriter StreamWriter { get; set; }
        public IDbQuery Query { get; set; }
        public String Result { get; set; }
        #endregion

        public DynaObject(QryDef qryDef)
        {
            qry_id = qryDef.qry_id;
            src_id = qryDef.col_def;
            qry_name = qryDef.qry_name;
            // def 45 = Idn | Det | Out | Usr
            if (qryDef.col_flags == 0) col_flags = 45;
            else col_flags = qryDef.col_flags;
            // select parameters
            ParmDict = new Dictionary<String, IDynaProp>(4);
            //dynamic properties
            PropDict = new Dictionary<String, IDynaProp>(16);
            //ordinal properties
            ReadList = new List<IDynaProp>(16);
            lockObj = new Object();
        }

        public void CreateParm(PamDef pamDef)
        {
            IDynaProp prop = null;
            switch (pamDef.pam_type)
            {
                case 167: //varchar
                    prop = new StringProp(pamDef.pam_name, DbType.String, pamDef.pam_size, 0);
                    break;
                case 175: //char[]->varchar
                    prop = new StringProp(pamDef.pam_name, DbType.String, pamDef.pam_size, 0);
                    break;
                case 48: //byte
                    prop = new ByteProp(pamDef.pam_name, DbType.Byte, pamDef.pam_size, 0);
                    break;
                case 52: //int16
                    prop = new Int16Prop(pamDef.pam_name, DbType.Int16, pamDef.pam_size, 0);
                    break;
                case 56: //int32
                    prop = new Int32Prop(pamDef.pam_name, DbType.Int32, pamDef.pam_size, 0);
                    break;
                case 40: //date
                    prop = new DateProp(pamDef.pam_name, DbType.Date, pamDef.pam_size, 0);
                    break;
                case 62: //double
                    prop = new DoubleProp(pamDef.pam_name, DbType.Double, pamDef.pam_size, 0);
                    break;
            }
            if (prop != null) ParmDict.Add(pamDef.pam_name, prop);
        }

        public void CreateProp(ColDef colDef)
        {
            IDynaProp prop = null;
            switch (colDef.col_type)
            {
                case 167: //varchar
                    prop = new StringProp(colDef.col_name, DbType.String, colDef.col_size, colDef.col_flags);
                    break;
                case 175: //char[]->varchar
                    prop = new StringProp(colDef.col_name, DbType.String, colDef.col_size, colDef.col_flags);
                    break;
                case 48: //byte
                    prop = new ByteProp(colDef.col_name, DbType.Byte, colDef.col_size, colDef.col_flags);
                    break;
                case 52: //int16
                    prop = new Int16Prop(colDef.col_name, DbType.Int16, colDef.col_size, colDef.col_flags);
                    break;
                case 56: //int32
                    prop = new Int32Prop(colDef.col_name, DbType.Int32, colDef.col_size, colDef.col_flags);
                    break;
                case 40: //date
                    prop = new DateProp(colDef.col_name, DbType.Date, colDef.col_size, colDef.col_flags);
                    break;
                case 62: //double
                    prop = new DoubleProp(colDef.col_name, DbType.Double, colDef.col_size, colDef.col_flags);
                    break;
            }
            if (prop != null) PropDict.Add(colDef.col_name, prop);
        }

        public void ReadPropStream(Stream stream, string cmd)
        {
            string key;
            int i_type;
            Dictionary<String, IDynaProp> dictionary;
            if (cmd == "sel") dictionary = ParmDict;
            else dictionary = PropDict;
            // read parameters
            StreamReader.Open(stream);
            while (StreamReader.Read())
            {
                i_type = StreamReader.TokenType();
                if (i_type == 4) // PropName
                {
                    key = StreamReader.Value() as string;
                    StreamReader.Read(); // read Value
                    if (dictionary.ContainsKey(key))
                        dictionary[key].Value = StreamReader.Value();
                }
            }
            StreamReader.Close();
        }

        private void ReadOrdinals(IDataReader reader, int flags)
        {
            string key;
            IDynaProp prop;
            int i_ord, i_count = reader.FieldCount;
            ReadList.Clear();
            foreach (var p in PropDict.Values) p.Ordinal = -1;
            for (i_ord = 0; i_ord < i_count; i_ord++)
            {
                key = reader.GetName(i_ord);
                if (PropDict.Keys.Contains<string>(key))
                {
                    prop = PropDict[key];
                    //read idn|sel fields for select
                    //read idn|sel|det|usr 39=1+2+4+32 fields for detail
                    if ((prop.GetFlags() & flags) > 0)
                    {
                        prop.Ordinal = i_ord;
                        ReadList.Add(prop);
                    }
                }
            }
        }

        public IDataReader Select()
        {
            IDataReader result = null;
            IDynaProp[] parms = ParmDict.Values.ToArray();
            result = Query.Select(parms);
            if (result != null) ReadOrdinals(result, 3);
            return result;
        }

        public IDataReader Detail(int idn)
        {
            IDataReader result = null;
            IDynaProp prop = PropDict["Idn"];
            prop.Value = idn;
            result = Query.Detail(prop);
            if (result != null) ReadOrdinals(result, 39);
            return result;
        }

        public IDynaProp[] Action(string cmd)
        {
            //default idn|det|out|usr 45=1+4+8+32
            var actProps = PropDict.Values.Where(prop => (prop.GetFlags() & col_flags) > 0).ToArray();
            Query.Action(actProps, cmd);
            Result = Query.Result;
            //properties with returned values
            return actProps.Where(prop => (prop.GetFlags() & 8) > 0).ToArray();
        }

        private void WriteRecord(IDataRecord record, List<IDynaProp> props, IPropWriter writer)
        {
            foreach (var prop in props)
                prop.WriteProp(record, writer);
        }

        public void SelectToStream(Stream stream)
        {
            lock (lockObj)
            {
                IDataReader selReader = null;
                try
                {
                    StreamWriter.Open(stream);
                    StreamWriter.PushObj();
                    StreamWriter.PushArrProp("selected");
                    DateTime fst = DateTime.Now;
                    selReader = Select();
                    if (selReader != null)
                    {
                        while (selReader.Read())
                        {
                            //if (token.IsCancellationRequested) break;
                            StreamWriter.PushObj();
                            WriteRecord(selReader, ReadList, StreamWriter);
                            StreamWriter.Pop();
                        }
                        selReader.Close();
                    };
                    DateTime lst = DateTime.Now;
                    TimeSpan ts = lst - fst;
                    StreamWriter.Pop();
                    StreamWriter.WriteProp("message", Query.Result);
                    StreamWriter.WriteProp("sel_time", DateTime.Now.ToShortTimeString());
                    StreamWriter.WriteProp("time_ms", ts.Milliseconds);
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

        public void DetailToStream(Stream stream, int idn)
        {
            lock (lockObj)
            {
                IDataReader detReader = null;
                IDataReader selReader = null;
                try
                {
                    StreamWriter.Open(stream);
                    StreamWriter.PushObj();
                    detReader = Detail(idn);
                    if (detReader != null)
                    {
                        StreamWriter.PushObjProp("det_row");
                        if (detReader.Read())
                            WriteRecord(detReader, ReadList, StreamWriter);
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
                        slave.ParmDict[qry_name].Value = idn;
                        selReader = slave.Select();
                        if (selReader != null)
                        {
                            while (selReader.Read())
                            {
                                StreamWriter.PushObj();
                                WriteRecord(selReader, slave.ReadList, StreamWriter);
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
                    var outProps = Action(cmd);
                    if (Query.Result != "Ok") throw new Exception(Query.Result);
                    //write output parameters
                    StreamWriter.PushObjProp("out");
                    foreach (IDynaProp prop in outProps)
                        prop.WriteProp(StreamWriter);
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

        public string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Id: {0}, Name: {1}\n", qry_id, qry_name);
            foreach (var pair in PropDict)
            {
                //sb.AppendLine(key);
                IDynaProp prop = pair.Value;
                sb.AppendFormat("Prop: {0}, Value: {1}\n", pair.Key, prop.Value);
            }
            sb.AppendFormat("Result: {0}\n", Result);
            return sb.ToString();
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
