using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Kobdik.Common;
using Kobdik.Dynamics;
using Newtonsoft.Json;

namespace Kobdik.DataModule
{
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

}