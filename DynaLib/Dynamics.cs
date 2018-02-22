using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Kobdik.Common;

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
            IDataParameter param = detComm.CreateParameter();
            param.ParameterName = String.Format("@{0}", prop.GetName());
            param.DbType = prop.GetDbType();
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

        public void Update(IDynaProp[] props)
        {
            string proName = "upd_" + qry_name;
            IDbCommand updComm = db_conn.CreateCommand();
            updComm.CommandType = CommandType.StoredProcedure;
            updComm.CommandText = proName;
            //fill update parameters
            foreach (IDynaProp prop in props)
            {
                IDataParameter param = updComm.CreateParameter();
                param.ParameterName = String.Format("@{0}", prop.GetName());
                param.DbType = prop.GetDbType();
                param.Value = prop.Value;
                if ((prop.GetFlags() & 8) > 0) param.Direction = ParameterDirection.InputOutput;
                updComm.Parameters.Add(param);
            }
            try
            {
                db_conn.Open();
                if (updComm.ExecuteNonQuery() == 0)
                {
                    if (updComm.Parameters.Contains("@Cause"))
                    {
                        IDataParameter param = updComm.Parameters["@Cause"] as IDataParameter;
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
                        IDataParameter param = updComm.Parameters[parameterName] as IDataParameter;
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

    public class DynaObject : IDynaObject
    {
        #region fields
        private string qry_name;
        private byte qry_id, src_id, act_flag;
        private object lockObj;
        #endregion fields

        #region properties
        public byte QryId { get { return qry_id; } }
        public string QryName { get { return qry_name; } }
        public Dictionary<String, IDynaProp> PropDict { get; set; }
        public Dictionary<String, IDynaProp> ParmDict { get; set; }
        public Dictionary<String, DynaObject> SlaveDict { get; set; }
        public IDbQuery Query { get; set; }
        public String Result { get; set; }
        #endregion

        public DynaObject(QryDef qryDef)
        {
            qry_id = qryDef.qry_id;
            src_id = qryDef.col_def;
            qry_name = qryDef.qry_name;
            // def 45 = Idn | Det | Out | Usr
            if (qryDef.col_flags == 0) act_flag = 45;
            else act_flag = qryDef.col_flags;
            // select parameters
            ParmDict = new Dictionary<String, IDynaProp>();
            //dynamic properties
            PropDict = new Dictionary<String, IDynaProp>();
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

        public IDynaProp[] Update()
        {
            var updProps = PropDict.Values.Where(prop => (prop.GetFlags() & act_flag) > 0).ToArray();
            Query.Update(updProps);
            Result = Query.Result;
            //properties with returned values
            return updProps.Where(prop => (prop.GetFlags() & 8) > 0).ToArray();
        }

        public void ReadOrdinals(IDataReader reader, int flags)
        {
            string key;
            IDynaProp prop;
            int i_ord, i_count = reader.FieldCount;
            foreach (var p in PropDict.Values) p.Ordinal = -1;
            for (i_ord = 0; i_ord < i_count; i_ord++)
            {
                key = reader.GetName(i_ord);
                if (PropDict.Keys.Contains<string>(key))
                {
                    prop = PropDict[key];
                    //read idn|sel fields for select
                    //read idn|sel|det|usr 39=1+2+4+32 fields for detail
                    //read out fields for update
                    if ((prop.GetFlags() & flags) > 0)
                        prop.Ordinal = i_ord;
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

    public class PropMap
    {
        public IDynaProp Prop => dynaProp;
        public MethodInfo GetMethod { get; set; }
        public MethodInfo SetMethod { get; set; }
        private object[] parameters;
        private IDynaProp dynaProp;

        public PropMap(IDynaProp prop)
        {
            dynaProp = prop;
            parameters = new object[1];
        }

        public void ReadToObject(IDataReader reader, object obj)
        {
            dynaProp.ReadProp(reader);
            if (SetMethod != null)
            {
                parameters[0] = dynaProp.Value;
                SetMethod.Invoke(obj, parameters);
            }
        }

        public void GetFromObject(object obj)
        {
            if (GetMethod != null)
                dynaProp.Value = GetMethod.Invoke(obj, null);
        }

        public void SetToObject(object obj)
        {
            if (SetMethod != null)
            {
                parameters[0] = dynaProp.Value;
                SetMethod.Invoke(obj, parameters);
            }
        }

    }

    public abstract class DynaQuery<T> : IEnumerable<T>, IEnumerator<T>, IEnumerable, IEnumerator
    {
        protected IDynaObject _dynaObject;
        protected IDataReader _dataReader;
        protected List<PropMap> propMaps;
        protected List<PropMap> sel_Maps;
        private List<T> list;
        private T _current;
        private Type _type;
        private bool cached, dbread;
        private int i_current;

        public DynaQuery(IDynaObject dynaObject)
        {
            _dynaObject = dynaObject;
            _current = Activator.CreateInstance<T>();
            _type = _current.GetType();
            propMaps = new List<PropMap>(dynaObject.PropDict.Count);
            sel_Maps = new List<PropMap>(dynaObject.PropDict.Count);
            list = new List<T>(1024);
            cached = false;
            dbread = true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            //throw new NotImplementedException();
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            Reset();
            return this;
        }

        object IEnumerator.Current => _current;

        public T Current => _current;

        public void UpdateCurrent()
        {
            if (dbread)
            {
                _current = Activator.CreateInstance<T>();
                foreach (var propMap in sel_Maps)
                    propMap.ReadToObject(_dataReader, _current);
                list.Add(_current);
            }
            else _current = list[i_current];
        }

        public bool MoveNext()
        {
            bool hasNext;
            if (dbread)
                hasNext = _dataReader.Read();
            else
                hasNext = list.Count > ++i_current;
            //move ahead and update current
            if (hasNext) UpdateCurrent();
            return hasNext;
        }

        public void Update(T t)
        {
            //считываем связанные свойства в _dynaObject
            foreach (var propMap in propMaps)
                propMap.GetFromObject(t);
            //отправляем изменения и получаем результаты
            var updProps = _dynaObject.Update();
            //обновляем связанные свойства по полученным результатам
            foreach (var propMap in propMaps.Where(bp => updProps.Contains(bp.Prop)))
                propMap.SetToObject(t);
        }

        public abstract void OnReset(string message);

        public void Reset()
        {
            string result = "Reset";
            try
            {
                if (!cached)
                {
                    _dataReader = _dynaObject.Select();
                    sel_Maps.Clear();
                    foreach (var propMap in propMaps.Where(pm => pm.Prop.Ordinal >= 0))
                        sel_Maps.Add(propMap);
                    cached = true;
                    dbread = true;
                }
                else
                {
                    i_current = -1;
                    dbread = false;
                }
                result = "Ok";
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            OnReset(result);
        }

        public void MapToCurrent(string dynaPropName, string currPropName)
        {
            PropertyInfo info = _type.GetProperty(currPropName);
            if (info == null || !_dynaObject.PropDict.ContainsKey(dynaPropName)) return;
            IDynaProp prop = _dynaObject.PropDict[dynaPropName];
            if (prop.GetPropType() != info.PropertyType) return;
            propMaps.Add(new PropMap(prop)
            {
                GetMethod = info.GetGetMethod(),
                SetMethod = info.GetSetMethod()
            });
        }

        public void ResetCache()
        {
            list.Clear();
            cached = false;
        }

        public void Dispose()
        {
            _dynaObject.Dispose();
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
