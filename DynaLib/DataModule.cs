using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Kobdik.Common;
using Kobdik.Dynamics;

namespace Kobdik.DataModule
{
    public delegate void NotifyEvent(String message);
    public delegate IDbConnection Connection();

    public class DataMod
    {
        #region fields
        public NotifyEvent ShowError, ShowMessage;
        public Connection GetConnection;
        public List<QryDef> qryList;
        public List<FldDef> fldList;
        public Dictionary<string, DynaRecord> recDict;
        public string lastError = "";
        public static bool loaded = false;
        private static DataMod dataMod;
        private object lockObj;
        #endregion fields

        static DataMod()
        {
            dataMod = new DataMod();
        }

        private DataMod() 
        {
            lockObj = new Object();
            qryList = new List<QryDef>(32);
            fldList = new List<FldDef>(128);
            recDict = new Dictionary<string, DynaRecord>(32);
        }

        public static DataMod Current() { return dataMod; }

        public bool LoadMeta()
        {
            loaded = false;
            IDbConnection dbConn = null;
            IDbCommand qryComm = null, fldComm = null;
            IDataReader qryReader = null, fldReader = null;
            try
            {
                recDict.Clear();
                //get connection from pool
                dbConn = GetConnection();
                dbConn.Open();
                //qryDict
                qryComm = dbConn.CreateCommand();
                qryComm.CommandType = CommandType.Text;
                qryComm.CommandText = "select Qry_Name, Qry_Head, Qry_Lord, Fld_Dict, Qry_Mask from T_QryDict";
                qryReader = qryComm.ExecuteReader();
                qryList.Clear();
                while (qryReader.Read())
                {
                    QryDef qryDef = new QryDef();
                    qryDef.qry_name = qryReader.GetString(0);
                    qryDef.qry_head = qryReader.GetString(1);
                    qryDef.qry_lord = qryReader.GetString(2);
                    qryDef.fld_dict = qryReader.GetString(3);
                    qryDef.qry_mask = qryReader.GetInt32(4);
                    qryList.Add(qryDef);
                }
                qryReader.Close();
                //fldList
                fldComm = dbConn.CreateCommand();
                fldComm.CommandType = CommandType.Text;
                fldComm.CommandText = "select Qry_Name, Fld_Name, Fld_Head, Fld_Type, Fld_Size, Inp_Mask, Out_Mask, Def_Val from T_FldDict";
                fldReader = fldComm.ExecuteReader();
                fldList.Clear();
                while (fldReader.Read())
                {
                    FldDef fldDef = new FldDef();
                    fldDef.qry_name = fldReader.GetString(0);
                    fldDef.fld_name = fldReader.GetString(1);
                    fldDef.fld_head = fldReader.GetString(2);
                    fldDef.fld_type = fldReader.GetInt32(3);
                    fldDef.fld_size = fldReader.GetInt32(4);
                    fldDef.inp_mask = fldReader.GetInt32(5);
                    fldDef.out_mask = fldReader.GetInt32(6);
                    fldDef.def_val = fldReader.GetString(7);
                    fldList.Add(fldDef);
                }
                fldReader.Close();
                loaded = true;
            }
            catch (DbException sx)
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
                if (fldReader != null) fldReader.Close();
                if (qryReader != null) qryReader.Close();
                if (dbConn != null) dbConn.Close();
            }
            return loaded;
        }

        public DynaRecord GetDynaRecord(string key)
        {
            lock (lockObj)
            {
                DynaRecord dynaRecord = null;
                //поискать в кэше объектов
                if (recDict.TryGetValue(key, out dynaRecord)) return dynaRecord;
                //иначе создать DynaRecord
                QryDef qryDef = qryList.Find(qry => qry.qry_name == key);
                if (qryDef == null)
                {
                    dataMod.lastError = string.Format("Запрос {0} не найден!", key);
                    return null;
                }
                dynaRecord = new DynaRecord(qryDef)
                {
                    //Адаптер для работы с DB
                    Query = new DataQuery(GetConnection(), key),
                    //Для чтения json-потока
                    StreamReader = new JsonStreamReader(),
                    //Для записи в json-поток
                    StreamWriter = new TextStreamWriter()
                };
                //загрузить описания всех колонок
                foreach (FldDef fldDef in fldList.Where(fld => fld.qry_name == qryDef.fld_dict))
                    dynaRecord.CreateField(fldDef);
                //добавить в кэш объектов
                recDict.Add(key, dynaRecord);
                return dynaRecord;
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
        private Encoding win1251 = Encoding.GetEncoding(1251);
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
                textWriter = new StreamWriter(stream, win1251);
                result = "Open stream writer..";
            }
            writer = new JsonTextWriter(textWriter);
            //writer.Culture = new CultureInfo("ru-RU");
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

        public void WriteProp(String propName)
        {
            writer.WritePropertyName(propName);
        }

        public void WriteProp(Byte[] value, int len, int state)
        {
            if ((state & 1) > 0)
                writer.WriteValue(win1251.GetString(value, 0, len));
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

    public class TextStreamWriter : IStreamWriter, IPropWriter
    {
        private Stack<byte> stack;
        private TextWriter writer;
        private StringBuilder builder;
        private Encoding win1251;
        private string result;
        private bool empty;
        //1-bin, 2-json, 3-xml
        public byte GetStreamType() { return 2; }

        public void Open(Stream stream)
        {
            if (stream == null)
            {
                builder = new StringBuilder();
                writer = new StringWriter(builder);
                result = "Open string writer..";
            }
            else
            {
                builder = null;
                writer = new StreamWriter(stream);
                result = "Open stream writer..";
            }
            //writer.Culture = new CultureInfo("ru-RU");
            win1251 = Encoding.GetEncoding(1251);
            stack = new Stack<byte>(4);
            empty = true;
        }

        public void PushArr()
        {
            if (empty)
                writer.Write('[');
            else
                writer.Write(",[");
            //Array container
            stack.Push(1);
            //in array context
            empty = true;
        }

        public void PushObj()
        {
            if (empty)
                writer.Write('{');
            else
                writer.Write(",{");
            //Object container
            stack.Push(2);
            //in object context
            empty = true;
        }

        public void PushArrProp(string propName)
        {
            if (empty)
                writer.Write(String.Format("\"{0}\":[", propName));
            else
                writer.Write(String.Format(",\"{0}\":[", propName));
            //Array property
            stack.Push(3);
            //in array context
            empty = true;
        }

        public void PushObjProp(string propName)
        {
            if (empty)
                writer.Write(String.Format("\"{0}\":{", propName));
            else
                writer.Write(String.Format(",\"{0}\":{", propName));
            //Object property
            stack.Push(4);
            //in object context
            empty = true;
        }

        public void Pop()
        {
            if (stack.Count == 0) return;
            byte top = stack.Pop();
            switch (top)
            {
                case 1: writer.Write("]"); break;
                case 2: writer.Write("}"); break;
                case 3: writer.Write("]"); break;
                case 4: writer.Write("}"); break;
            }
            empty = false;
        }

        public void WriteProp(String propName)
        {
            if (empty)
            {
                writer.Write("\"");
                //writer.Write("\"{0}\":\"", propName);
                empty = false;
            }
            else
            {
                writer.Write(",\"");
                //writer.Write(",\"{0}\":\"", propName);
                empty = false;
            }
            writer.Write(propName);
            writer.Write("\":");
        }

        public void WriteProp(Byte[] value, int len, int state)
        {
            if ((state & 1) > 0) writer.Write('\"');
            writer.Write(win1251.GetString(value, 0, len));
            if ((state & 2) > 0) writer.Write('\"');
        }

        public void WriteProp(String propName, String value)
        {
            if (empty)
            {
                writer.Write("\"");
                //writer.Write("\"{0}\":\"{1}\"", propName, value);
                empty = false;
            }
            else
            {
                writer.Write(",\"");
                //writer.Write(",\"{0}\":\"{1}\"", propName, value);
            }
            writer.Write(propName);
            writer.Write("\":\"");
            writer.Write(value);
            writer.Write("\"");
        }

        public void WriteProp(String propName, Byte value)
        {
            if (empty)
            {
                writer.Write("\"");
                //writer.Write("\"{0}\":{1}", propName, value);
                empty = false;
            }
            else
            {
                writer.Write(",\"");
                //writer.Write(",\"{0}\":{1}", propName, value);
            }
            writer.Write(propName);
            writer.Write("\":");
            writer.Write(value);
        }

        public void WriteProp(String propName, Int16 value)
        {
            if (empty)
            {
                writer.Write("\"");
                //writer.Write("\"{0}\":{1}", propName, value);
                empty = false;
            }
            else
            {
                writer.Write(",\"");
                //writer.Write(",\"{0}\":{1}", propName, value);
            }
            writer.Write(propName);
            writer.Write("\":");
            writer.Write(value);
        }

        public void WriteProp(String propName, Int32 value)
        {
            if (empty)
            {
                writer.Write("\"");
                //writer.Write("\"{0}\":{1}", propName, value);
                empty = false;
            }
            else
            {
                writer.Write(",\"");
                //writer.Write(",\"{0}\":{1}", propName, value);
            }
            writer.Write(propName);
            writer.Write("\":");
            writer.Write(value);
        }

        public void WriteProp(String propName, DateTime value)
        {
            int dec;
            if (empty)
            {
                writer.Write('\"');
                //writer.Write("\"{0}\":\"{1}-{2:D2}-{3:D2}T00:00:00\"", propName, value.Year, value.Month, value.Day);
                empty = false;
            }
            else
            {
                writer.Write(",\"");
                //writer.Write(",\"{0}\":\"{1}-{2:D2}-{3:D2}T00:00:00\"", propName, value.Year, value.Month, value.Day);
            }
            writer.Write(propName);
            writer.Write("\":\"");
            writer.Write(value.Year);
            writer.Write('-');
            dec = value.Month;
            if (dec < 10) writer.Write('0');
            writer.Write(dec);
            writer.Write('-');
            dec = value.Day;
            if (dec < 10) writer.Write('0');
            writer.Write(dec);
            writer.Write("T00:00:00\"");
        }

        public void WriteProp(String propName, Double value)
        {
            if (empty)
            {
                writer.Write("\"");
                //writer.Write("\"{0}\":{1}", propName, value);
                empty = false;
            }
            else
            {
                writer.Write(",\"");
                //writer.Write(",\"{0}\":{1}", propName, value);
            }
            writer.Write(propName);
            writer.Write("\":");
            writer.Write(value);
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
        public IDynaField Field => dynaField;
        public MethodInfo GetMethod { get; set; }
        public MethodInfo SetMethod { get; set; }
        private object[] parameters;
        private IDynaField dynaField;

        public PropMap(IDynaField field)
        {
            dynaField = field;
            parameters = new object[1];
        }

        public void ReadToObject(IDataReader reader, object obj)
        {
            dynaField.ReadProp(reader);
            if (SetMethod != null)
            {
                parameters[0] = dynaField.Value;
                SetMethod.Invoke(obj, parameters);
            }
        }

        public void GetFromObject(object obj)
        {
            if (GetMethod != null)
                dynaField.Value = GetMethod.Invoke(obj, null);
        }

        public void SetToObject(object obj)
        {
            if (SetMethod != null)
            {
                parameters[0] = dynaField.Value;
                SetMethod.Invoke(obj, parameters);
            }
        }

    }

    public abstract class DynaQuery<T> : IEnumerable<T>, IEnumerator<T>, IEnumerable, IEnumerator
    {
        protected IDynaRecord _dynaRecord;
        protected IDataReader _dataReader;
        protected List<PropMap> propMaps;
        protected List<PropMap> sel_Maps;
        private IDataCommand _dataCmd;
        private List<T> list;
        private T _current;
        private Type _type;
        private bool cached, dbread;
        private int i_current;

        public DynaQuery(IDynaRecord dynaRecord)
        {
            _dynaRecord = dynaRecord;
            _dataCmd = dynaRecord as IDataCommand;
            _current = Activator.CreateInstance<T>();
            _type = _current.GetType();
            propMaps = new List<PropMap>(16);
            sel_Maps = new List<PropMap>(16);
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

        public void Action(T t, string cmd)
        {
            //считываем связанные свойства в _dynaObject
            foreach (var propMap in propMaps)
                propMap.GetFromObject(t);
            //отправляем изменения и получаем результаты
            var outProps = _dataCmd.Action(cmd);
            //обновляем связанные свойства по полученным результатам
            foreach (var propMap in propMaps.Where(bp => outProps.Contains(bp.Field)))
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
                    _dataReader = _dataCmd.Select();
                    if (_dataReader != null)
                    {
                        sel_Maps.Clear();
                        foreach (var propMap in propMaps.Where(pm => pm.Field.Ordinal >= 0)) sel_Maps.Add(propMap);
                        sel_Maps.Sort((l, r) => l.Field.Ordinal - r.Field.Ordinal);
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

        public void AutoMapProps(int cmd_bit)
        {
            foreach (var pair in _dynaRecord.FieldDict)
            {
                IDynaField field = pair.Value;
                if ((field.GetOutMask() & cmd_bit) > 0) MapToCurrent(pair.Key, pair.Key);
            }
        }

        public void MapToCurrent(string fieldName, string propName)
        {
            PropertyInfo info = _type.GetProperty(propName);
            if (info == null || !_dynaRecord.FieldDict.ContainsKey(fieldName)) return;
            IDynaField field = _dynaRecord.FieldDict[fieldName];
            if (field.GetPropType() != info.PropertyType) return;
            propMaps.Add(new PropMap(field)
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
            _dynaRecord.Dispose();
        }

    }

}