using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Kobdik.Common;
using Kobdik.Dynamics;
using Newtonsoft.Json;

namespace Kobdik.DataModule
{
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

    public delegate void NotifyEvent(String message);

    public class DataMod
    {
        #region fields
        public NotifyEvent ShowError, ShowMessage;
        public List<QryDef> qryList;
        public List<PamDef> pamList;
        public List<ColDef> colList;
        public Dictionary<string, DynaObject> objDict;
        public string lastError = "";
        public static bool loaded = false;
        private static DataMod dataMod;
        private string connString;
        private object lockObj;
        #endregion fields

        static DataMod()
        {
            dataMod = new DataMod();
            loaded = dataMod.LoadMeta();
        }

        private DataMod() 
        {
            lockObj = new Object();
            qryList = new List<QryDef>(32);
            pamList = new List<PamDef>(32);
            colList = new List<ColDef>(128);
            objDict = new Dictionary<string, DynaObject>(16);
            connString = ConfigurationManager.AppSettings["Conn"];
        }

        public static DataMod Current() { return dataMod; }

        private IDbConnection GetConnection()
        {
            return new SqlConnection
            {
                //Connection, определяемый строкой соединения
                ConnectionString = connString
            };
        }

        private bool LoadMeta()
        {
            bool result = false;
            SqlConnection sqlConn = null;
            SqlCommand qryComm = null, pamComm = null, colComm = null;
            DbDataReader qryReader = null, pamReader = null, colReader = null;
            try
            {
                //get connection from pool
                sqlConn = new SqlConnection();
                sqlConn.ConnectionString = connString;
                sqlConn.Open();
                //qryDict
                qryComm = new SqlCommand("select Qry_Id, Qry_Name, Qry_Head, Col_Def, Col_Flag from T_QryDict", sqlConn);
                qryComm.CommandType = CommandType.Text;
                qryReader = qryComm.ExecuteReader();
                while (qryReader.Read())
                {
                    QryDef qryDef = new QryDef();
                    qryDef.qry_id = qryReader.GetByte(0);
                    qryDef.qry_name = qryReader.GetString(1);
                    qryDef.qry_head = qryReader.GetString(2);
                    qryDef.col_def = qryReader.GetByte(3);
                    qryDef.col_flags = qryReader.GetByte(4);
                    qryList.Add(qryDef);
                }
                qryReader.Close();
                //pamDict
                pamComm = new SqlCommand("select Qry_Id, Pam_Id, Pam_Name, Pam_Type, Pam_Size, Def_Val from T_PamDict", sqlConn);
                pamComm.CommandType = CommandType.Text;
                pamReader = pamComm.ExecuteReader();
                while (pamReader.Read())
                {
                    PamDef pamDef = new PamDef();
                    pamDef.qry_id = pamReader.GetByte(0);
                    pamDef.pam_id = pamReader.GetByte(1);
                    pamDef.pam_name = pamReader.GetString(2);
                    pamDef.pam_type = pamReader.GetByte(3);
                    pamDef.pam_size = pamReader.GetInt16(4);
                    pamDef.def_val = pamReader.GetString(5);
                    pamList.Add(pamDef);
                }
                pamReader.Close();
                //colDict
                colComm = new SqlCommand("select Qry_Id, Col_Id, Col_Name, Col_Head, Col_Type, Col_Size, Col_Flag from T_ColDict", sqlConn);
                colComm.CommandType = CommandType.Text;
                colReader = colComm.ExecuteReader();
                while (colReader.Read())
                {
                    ColDef colDef = new ColDef();
                    colDef.qry_id = colReader.GetByte(0);
                    colDef.col_id = colReader.GetByte(1);
                    colDef.col_name = colReader.GetString(2);
                    colDef.col_head = colReader.GetString(3);
                    colDef.col_type = colReader.GetByte(4);
                    colDef.col_size = colReader.GetInt16(5);
                    colDef.col_flags = colReader.GetByte(6);
                    colList.Add(colDef);
                }
                colReader.Close();
                result = true;
            }
            catch (SqlException sx)
            {
                //if (ShowError != null) ShowError(sx.Message);
                lastError = sx.Message;
            }
            catch (Exception ex)
            {
                //if (ShowError != null) ShowError(ex.Message);
                lastError = ex.Message;
            }
            finally
            {
                if (colReader != null) colReader.Close();
                if (pamReader != null) pamReader.Close();
                if (qryReader != null) qryReader.Close();
                if (sqlConn != null) sqlConn.Close();
            }
            return result;
        }

        public DynaObject GetDynaObject(string key) 
        {
            lock (lockObj)
            {
                DynaObject dynaObject = null;
                //поискать в кэше объектов
                if (objDict.TryGetValue(key, out dynaObject)) return dynaObject;
                //иначе создать DynaObject
                byte qryId = (byte)qryList.FindIndex(q => q.qry_name == key);
                if (qryId == 255)
                {
                    dataMod.lastError = string.Format("Запрос {0} не найден!", key);
                    return null;
                }
                QryDef qryDef = qryList[qryId];
                dynaObject = new DynaObject(qryDef)
                {
                    //Адаптер для работы с DB
                    Query = new DbQuery(GetConnection(), key),
                    //Для чтения json-потока
                    StreamReader = new JsonStreamReader(),
                    //Для записи в json-поток
                    StreamWriter = new JsonStreamWriter()
                };
                //загрузить описание параметров select-запроса
                foreach (PamDef pamDef in pamList.Where(p => p.qry_id == qryDef.qry_id))
                    dynaObject.CreateParm(pamDef);
                //загрузить описания всех колонок
                foreach (ColDef colDef in colList.Where(c => c.qry_id == qryDef.col_def))
                    dynaObject.CreateProp(colDef);
                //добавить в кэш объектов
                objDict.Add(key, dynaObject);
                return dynaObject;
            }
        }

    }

    public class JsonStreamReader : IStreamReader
    {
        private JsonTextReader reader;
        
        //1-bin, 2-json, 3-xml
        public byte GetStreamType() { return 2; }

        public void Open(Stream stream)
        {
            TextReader textReader = new StreamReader(stream);
            reader = new JsonTextReader(textReader);
        }

        public bool Read()
        {
            return (reader != null) ? reader.Read() : false;
        }

        public int TokenType()
        {
            return (reader != null) ? (int)reader.TokenType : 0;
        }

        public Type ReadType()
        {
            return (reader != null) ? reader.ValueType : null;
        }

        public string ReadString()
        {
            return (reader != null) ? reader.ReadAsString() : null;
        }

        public int? ReadInt()
        {
            return (reader != null) ? reader.ReadAsInt32() : null;
        }

        public DateTime? ReadDateTime()
        {
            return (reader != null) ? reader.ReadAsDateTime() : null;
        }

        public Decimal? ReadDecimal()
        {
            return (reader != null) ? reader.ReadAsDecimal() : null;
        }

        public object Value()
        {
            return (reader != null) ? reader.Value : null;
        }

        public void Close()
        {
            reader?.Close();
        }

    }

    public class JsonStreamWriter : IStreamWriter, IPropWriter
    {
        private Stack<byte> stack;
        private JsonTextWriter writer;
        private StringBuilder builder;
        private string result;
        //1-bin, 2-json, 3-xml
        public byte GetStreamType() { return 2; }

        public void Open(Stream stream)
        {
            TextWriter textWriter = null;
            if (stream == null)
            {
                builder = new StringBuilder();
                textWriter = new StringWriter(builder);
                result = "Open string writer..";
            }
            else
            {
                builder = null;
                textWriter = new StreamWriter(stream);
                result = "Open stream writer..";
            }
            writer = new JsonTextWriter(textWriter);
            stack = new Stack<byte>();
        }

        public void PushArr()
        {
            writer.WriteStartArray();
            stack.Push(1);
        }

        public void PushObj()
        {
            writer.WriteStartObject();
            stack.Push(2);
        }

        public void PushArrProp(string propName)
        {
            writer.WritePropertyName(propName);
            writer.WriteStartArray();
            stack.Push(3);
        }

        public void PushObjProp(string propName)
        {
            writer.WritePropertyName(propName);
            writer.WriteStartObject();
            stack.Push(4);
        }

        public void Pop()
        {
            if (stack.Count == 0) return;
            byte top = stack.Pop();
            switch (top)
            {
                case 1: writer.WriteEndArray(); break;
                case 2: writer.WriteEndObject(); break;
                case 3: writer.WriteEndArray(); break;
                case 4: writer.WriteEndObject(); break;
            }
        }

        public void WriteProp(String propName, String value)
        {
            writer.WritePropertyName(propName);
            writer.WriteValue(value);
        }

        public void WriteProp(String propName, Byte value)
        {
            writer.WritePropertyName(propName);
            writer.WriteValue(value);
        }

        public void WriteProp(String propName, Int16 value)
        {
            writer.WritePropertyName(propName);
            writer.WriteValue(value);
        }

        public void WriteProp(String propName, Int32 value)
        {
            writer.WritePropertyName(propName);
            writer.WriteValue(value);
        }

        public void WriteProp(String propName, DateTime value)
        {
            writer.WritePropertyName(propName);
            writer.WriteValue(value);
        }

        public void WriteProp(String propName, Double value)
        {
            writer.WritePropertyName(propName);
            writer.WriteValue(value);
        }

        public void Close()
        {
            try
            {
                while (stack.Count > 0) Pop();
                if (builder != null)
                    result = builder.ToString();
            }
            catch (Exception) { }
            finally
            {
                writer.Flush();
                writer.Close();
            }
        }

        public string Result => result;
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
        private IDynaCommand _dynaCmd;
        private List<T> list;
        private T _current;
        private Type _type;
        private bool cached, dbread;
        private int i_current;

        public DynaQuery(IDynaObject dynaObject)
        {
            _dynaObject = dynaObject;
            _dynaCmd = dynaObject as IDynaCommand;
            _current = Activator.CreateInstance<T>();
            _type = _current.GetType();
            propMaps = new List<PropMap>(dynaObject.PropDict.Count);
            sel_Maps = new List<PropMap>(dynaObject.PropDict.Count);
            list = new List<T>(1024);
            cached = false;
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
            var outProps = _dynaCmd.Action("upd");
            //обновляем связанные свойства по полученным результатам
            foreach (var propMap in propMaps.Where(bp => outProps.Contains(bp.Prop)))
                propMap.SetToObject(t);
        }

        public abstract void OnReset(string message);

        public void Reset()
        {
            string result = "Reset";
            try
            {
                i_current = -1;
                dbread = false;
                if (!cached)
                {
                    _dataReader = _dynaCmd.Select();
                    if (_dataReader != null)
                    {
                        sel_Maps.Clear();
                        foreach (var propMap in propMaps.Where(pm => pm.Prop.Ordinal >= 0)) sel_Maps.Add(propMap);
                        dbread = true;
                    }
                    cached = true;
                }
                result = "Ok";
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            OnReset(result);
        }

        public void AutoMapProps(int flags)
        {
            foreach (var pair in _dynaObject.PropDict)
            {
                IDynaProp prop = pair.Value;
                if ((prop.GetFlags() & flags) > 0) MapToCurrent(pair.Key, pair.Key);
            }
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