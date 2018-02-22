using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using Kobdik.Common;
using Kobdik.Dynamics;

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
                    Query = new DbQuery(GetConnection(), key)
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

}