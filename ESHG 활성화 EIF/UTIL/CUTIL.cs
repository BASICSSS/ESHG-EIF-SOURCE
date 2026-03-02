using System;
using System.Data;
using System.Collections.Generic;

using LGCNS.ezControl.Common;
using LGCNS.ezControl.Core;
using LGCNS.ezControl.EIF.Solace;
using LGCNS.ezControl.Diagnostics;

using ESHG.EIF.FORM.COMMON;
using System.Data.SqlClient;
using LGCNS.ezControl.Data;

namespace ESHG.EIF.FORM.UTIL
{
    public partial class CUTIL : CSolaceEIFServerBizRule
    {
        #region Class Member variable
        private int ElecStartAddr = 0x5580; //전력량계 시작 Address
        private int FlowStartAddr = 0x55D0; //유량계 시작 Address

        List<CEUMInfo> PcUtilList = new List<CEUMInfo>();
        List<CEUMInfo> PlcUtilList = new List<CEUMInfo>();

        private SolaceElement EUMSolace = null;
        #endregion

        #region FactovaLync Method Override
        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            __INTERNAL_VARIABLE_STRING("V_W_EQP_ID", "EQP_INFO", enumAccessType.Virtual, false, true, "EIF_UTIL_SERVER", string.Empty, "EQP ID");

            __INTERNAL_VARIABLE_STRING("V_REQQUEUE_NAME", "BIZINFO", enumAccessType.Virtual, false, true, "", "", "EIF -> Biz Server Request Queue Name");
            __INTERNAL_VARIABLE_INTEGER("V_BIZCALL_TIMEOUT", "BIZINFO", enumAccessType.Virtual, 0, 0, true, false, 30000, string.Empty, "Biz Call TimeOut(mSec)");

            #region Virtual
            #region PC
            __INTERNAL_VARIABLE_INTEGER("V_SCAN_INTERVAL", "UTIL_INFO_PC", enumAccessType.Virtual, 0, 0, false, true, 60, "", "Util Data Scan Interval(Sec) - 0: Not Use");
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_UTILLOG_USE", "UTIL_INFO_PC", enumAccessType.Virtual, true, false, false, "", "True - Util Log 남김, False - Log 사용 안함");
            //__INTERNAL_VARIABLE_BOOLEAN("V_IS_RELOAD_INFO", "UTIL_INFO_PC", enumAccessType.Virtual, true, false, false, "", "True - 기준정보 Reload, False - 기준 정보 Reload후 자동 Off ");
            #endregion

            #region PLC
            __INTERNAL_VARIABLE_INTEGER("V_SCAN_INTERVAL", "UTIL_INFO_PLC", enumAccessType.Virtual, 0, 0, false, true, 10, "", "Util Data Scan Interval(Sec) - 0: Not Use");
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_UTILLOG_USE", "UTIL_INFO_PLC", enumAccessType.Virtual, true, false, false, "", "True - Util Log 남김, False - Log 사용 안함");
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_EM_USE_FLAG", "UTIL_INFO_PLC", enumAccessType.Virtual, true, false, true, "", "Power Measure Use Flag : True - USE [O], False - USE [X]");
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_FM_USE_FLAG", "UTIL_INFO_PLC", enumAccessType.Virtual, true, false, true, "", "Flow Measure Use Flag : True - USE [O], False - USE [X]");
            #endregion
            #endregion

            #region DB Data Load
            try
            {
                //$ 2025.07.21 : Command Line Argument를 사용하여 Line 정보를 구분
                string[] arguments = Environment.GetCommandLineArgs();
                string lineID = arguments[3].ToString();

                int iLineID = 0;
                if (!int.TryParse(lineID, out iLineID))
                {
                    this.Log(Level.Debug, $"Line ID : {lineID} ===> LINE ID IS NOT A NUMERIC TYPE!!!", this.EQP_INFO__V_W_EQP_ID, this.EQP_INFO__V_W_EQP_ID);
                    return;
                }

                LoadUtilInfo(iLineID);

                __INTERNAL_VARIABLE_STRING("V_LINE_ID", "BASICINFO", enumAccessType.Virtual, false, true, "", $"{lineID}", "LINEID - ex) 1, 2, 3 ");
            }
            catch { }
            #endregion

            #region PC
            foreach (var item in this.PcUtilList)
            {
                for (int serverCnt = 1; serverCnt <= item.UTIL_SERV_CNT; serverCnt++)
                {
                    //HOST->EUM
                    for (int measCnt = 1; measCnt <= item.UTIL_MEAS_CNT; measCnt++)
                    {
                        for (int paraCnt = 1; paraCnt <= item.UTIL_PARA_CNT; paraCnt++)
                        {
                            if (item.MointoringIdx != null && !item.MointoringIdx.Contains(paraCnt.ToString())) continue; //index가 null인 경우 전체, index에 포함 안됨 경우 처리 안함

                            if (item.UTILTYPE == EUMTYPE.ELEC)
                            {
                                if (paraCnt == 7 || paraCnt == 8 || paraCnt == 9)
                                    __INTERNAL_VARIABLE_INTEGER($"V_PC_EM_{measCnt:D2}_{paraCnt:D4}_{serverCnt:D2}", $"{item.EQPID}", enumAccessType.Virtual, 0, 0, false, false, 0, "", $"UTILITY_{measCnt:D2}_OUTDATA{paraCnt:D4}_{serverCnt:D2}");
                                else
                                    __INTERNAL_VARIABLE_SHORT($"V_PC_EM_{measCnt:D2}_{paraCnt:D4}_{serverCnt:D2}", $"{item.EQPID}", enumAccessType.Virtual, 0, 0, false, false, 0, "", $"UTILITY_{measCnt:D2}_OUTDATA{paraCnt:D4}_{serverCnt:D2}");
                            }
                            else if (item.UTILTYPE == EUMTYPE.FLOW)
                                __INTERNAL_VARIABLE_INTEGER($"V_PC_FW_{measCnt:D2}_{paraCnt:D4}_{serverCnt:D2}", $"{item.EQPID}", enumAccessType.Virtual, 0, 0, false, false, 0, "", $"UTILITY_{measCnt:D2}_OUTDATA{paraCnt:D4}_{serverCnt:D2}");
                        }
                    }
                }
            }
            #endregion

            #region PLC            

            #region IO
            foreach (var item in this.PlcUtilList)
            {
                for (int serverCnt = 1; serverCnt <= item.UTIL_SERV_CNT; serverCnt++)
                {
                    for (int measCnt = 1; measCnt <= item.UTIL_MEAS_CNT; measCnt++)
                    {
                        for (int paraCnt = 1; paraCnt <= item.UTIL_PARA_CNT; paraCnt++)
                        {
                            if (item.MointoringIdx != null && !item.MointoringIdx.Contains(paraCnt.ToString())) continue;

                            int tempAddr = 0;
                            if (item.UTILTYPE == EUMTYPE.ELEC)
                            {
                                switch (paraCnt)
                                {
                                    case 7: //주소값 +2
                                        tempAddr = this.ElecStartAddr + paraCnt - 1 + (measCnt - 1) * 11;
                                        __INTERNAL_VARIABLE_INTEGER($"I_W_PLC_EM_{measCnt:D2}_{paraCnt:D4}_{serverCnt:D2}", $"{item.EQPID}", enumAccessType.In, 0, 0, false, false, 0, $"DEVICE_TYPE=W,ADDRESS_NO={Convert.ToString(tempAddr, 16).ToUpper()}", $"UTILITY_EM_{measCnt:D2}_INDATA{paraCnt:D4}_{serverCnt:D2}");
                                        break;
                                    case 8:
                                        tempAddr = this.ElecStartAddr + paraCnt + (measCnt - 1) * 11;
                                        __INTERNAL_VARIABLE_INTEGER($"I_W_PLC_EM_{measCnt:D2}_{paraCnt:D4}_{serverCnt:D2}", $"{item.EQPID}", enumAccessType.In, 0, 0, false, false, 0, $"DEVICE_TYPE=W,ADDRESS_NO={Convert.ToString(tempAddr, 16).ToUpper()}", $"UTILITY_EM_{measCnt:D2}_INDATA{paraCnt:D4}_{serverCnt:D2}");
                                        break;
                                    default://주소값 +1
                                        tempAddr = this.ElecStartAddr + paraCnt + 1 + (measCnt - 1) * 11;
                                        __INTERNAL_VARIABLE_SHORT($"I_W_PLC_EM_{measCnt:D2}_{paraCnt:D4}_{serverCnt:D2}", $"{item.EQPID}", enumAccessType.In, 0, 0, false, false, 0, $"DEVICE_TYPE=W,ADDRESS_NO={Convert.ToString(tempAddr, 16).ToUpper()}", $"UTILITY_EM_{measCnt:D2}_INDATA{paraCnt:D4}_{serverCnt:D2}");
                                        break;
                                }
                            }
                            else
                            {
                                tempAddr = this.FlowStartAddr + (paraCnt - 1) * 2 + (measCnt - 1) * 4;
                                __INTERNAL_VARIABLE_INTEGER($"I_W_PLC_FW_{measCnt:D2}_{paraCnt:D4}_{serverCnt:D2}", $"{item.EQPID}", enumAccessType.In, 0, 0, false, false, 0, $"DEVICE_TYPE=W,ADDRESS_NO={Convert.ToString(tempAddr, 16).ToUpper()}", $"UTILITY_FM_{measCnt:D2}_INDATA{paraCnt:D4}_{serverCnt:D2}");
                            }
                        }
                    }
                }
            }
            #endregion
            #endregion
        }

        protected override void OnInitializeCompleted()
        {
            base.OnInitializeCompleted();

            HandleEmptyStringByNull = true;

            // CSolaceDevice 상속 받아 Solace Connect 추가 하는 Case           
            if (this.Elements.Count > 0)
            {
                if (this.Elements.ContainsKey("SOL"))
                {
                    this.EUMSolace = (SolaceElement)this.Elements["SOL"];

                    if (this.EUMSolace == null)
                        this.Log(Level.Debug, $"Solace element not registered", this.EQP_INFO__V_W_EQP_ID, this.EQP_INFO__V_W_EQP_ID);

                    //this.EUMSolace.OnSolaceMessageReceived += OnSolaceMessageReceived;
                }
            }
        }
        #endregion

        #region Thread Method
        //PLC Type의 경우 Interval로 Log값남기기
        [ManagedFunction(true)]
        private void UtilityPLCScan()
        {
            while (!IsFunctionAborted())
            {
                if (this.UTIL_INFO_PLC__V_SCAN_INTERVAL == 0)
                {
                    Wait(60000);
                    continue;
                }

                Wait(this.UTIL_INFO_PLC__V_SCAN_INTERVAL * 1000);

                try
                {
                    #region PLC EUM Data 수집
                    foreach (var item in this.PlcUtilList) // PLC LIST 만큼 가져옴 
                    {
                        int pow = 0;        //전력
                        int watt = 0;       //전력량
                        ushort fact = 0;    //역률

                        int INST = 0;       //순시유량
                        int INTG = 0;       //적산유량
                        for (int servCnt = 1; servCnt <= item.UTIL_SERV_CNT; servCnt++)
                        {
                            for (int measCnt = 1; measCnt <= item.UTIL_MEAS_CNT; measCnt++)
                            {
                                if (item.MointoringIdx != null)
                                {
                                    #region 특정 모니터링 대상
                                    foreach (string Idx in item.MointoringIdx)
                                    {
                                        if (item.UTILTYPE == EUMTYPE.ELEC)
                                        {
                                            switch (Convert.ToUInt16(Idx))
                                            {
                                                case 7:
                                                    pow = this.Variables[$"{item.EQPID}:I_W_PLC_EM_{measCnt:D2}_000{Idx}_{servCnt:D2}"].AsInteger;
                                                    break;
                                                case 8:
                                                    watt = this.Variables[$"{item.EQPID}:I_W_PLC_EM_{measCnt:D2}_000{Idx}_{servCnt:D2}"].AsInteger;
                                                    break;
                                                case 9:
                                                    fact = this.Variables[$"{item.EQPID}:I_W_PLC_EM_{measCnt:D2}_000{Idx}_{servCnt:D2}"].AsShort;
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            switch (Convert.ToUInt16(Idx))
                                            {
                                                case 1:
                                                    INST = this.Variables[$"{item.EQPID}:I_W_PLC_FW_{measCnt:D2}_000{Idx}_{servCnt:D2}"].AsInteger;
                                                    break;
                                                case 2:
                                                    INTG = this.Variables[$"{item.EQPID}:I_W_PLC_FW_{measCnt:D2}_000{Idx}_{servCnt:D2}"].AsInteger;
                                                    break;
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                else
                                {
                                    #region 전체 모니터링 대상
                                    if (item.UTILTYPE == EUMTYPE.ELEC)
                                    {
                                        pow = this.Variables[$"{item.EQPID}:I_W_PLC_EM_{measCnt:D2}_0007_{servCnt:D2}"].AsInteger;
                                        watt = this.Variables[$"{item.EQPID}:I_W_PLC_EM_{measCnt:D2}_0008_{servCnt:D2}"].AsInteger;
                                        fact = this.Variables[$"{item.EQPID}:I_W_PLC_EM_{measCnt:D2}_0009_{servCnt:D2}"].AsShort;
                                    }
                                    else if (item.UTILTYPE == EUMTYPE.FLOW)
                                    {
                                        INST = this.Variables[$"{item.EQPID}:I_W_PLC_FW_{measCnt:D2}_0001_{servCnt:D2}"].AsInteger;
                                        INTG = this.Variables[$"{item.EQPID}:I_W_PLC_FW_{measCnt:D2}_0002_{servCnt:D2}"].AsInteger;
                                    }
                                    #endregion
                                }

                                if (this.UTIL_INFO_PLC__V_IS_UTILLOG_USE)
                                {
                                    if (item.UTILTYPE == EUMTYPE.ELEC)
                                        this.Log(Level.Verbose, $"{item.EQPID} - {item.UTILTYPE}:{measCnt} : Power = {(float)pow / 1000:f3}kW, Watt = {watt}kWh, Factor = {fact}%", this.EQP_INFO__V_W_EQP_ID, item.EQPID);
                                    else if (item.UTILTYPE == EUMTYPE.FLOW)
                                        this.Log(Level.Verbose, $"{item.EQPID} - {item.UTILTYPE}:{measCnt} : INST - {INST}l, INTG - {INTG}l/min", this.EQP_INFO__V_W_EQP_ID, item.EQPID);
                                }

                                //EUMSolace.SendQueue(this.BIZINFO__V_REQQUEUE_NAME, "AAAAAAA");

                                Wait(100);
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    this.Log(Level.Debug, $"UTIL PLC DATA COLLECT Exception : {ex.ToString()}", this.EQP_INFO__V_W_EQP_ID, this.EQP_INFO__V_W_EQP_ID);
                }
            }
        }

        //PC Type의 경우 Interval로 DB값 가져오기
        [ManagedFunction(true)]
        private void UtilityPCScan()
        {
            while (!IsFunctionAborted())
            {
                if (this.UTIL_INFO_PC__V_SCAN_INTERVAL == 0)
                {
                    Wait(60000);
                    continue;
                }

                Wait(this.UTIL_INFO_PC__V_SCAN_INTERVAL * 1000);

                try
                {
                    foreach (var item in this.PcUtilList) // PC LIST 만큼 가져옴 
                    {
                        int pow = 0;        //전력
                        int watt = 0;       //전력량
                        int fact = 0;    //역률

                        int INST = 0;       //순시유량
                        int INTG = 0;       //적산유량

                        #region PC EUM Data 수집을 위한 DB Query
                        DataSet ds = new DataSet();

                        string strSQL = string.Format("SELECT * FROM TB_UTIL_MEASR WHERE EQPTID = '{0}' AND UTIL_TYPE = '{1}' ORDER BY UPDATED_DATE", item.EQPID, item.UTILTYPE);

                        SqlConnection con = new SqlConnection();
                        using (CDataManager mgr = new CDataManager())
                        {
                            mgr.GetDataSet(ds, "TB_UTIL_MEASR", strSQL);
                        }

                        if (ds.Tables.Contains("TB_UTIL_MEASR") == false)
                            return;

                        DataTable dt = ds.Tables["TB_UTIL_MEASR"];
                        #endregion

                        #region PLC EUM Data 수집 (EIF DB)
                        for (int servCnt = 1; servCnt <= item.UTIL_SERV_CNT; servCnt++)
                        {
                            for (int measCnt = 0; measCnt < dt.Rows.Count; measCnt++)
                            {
                                DataRow row = dt.Rows[measCnt];

                                if (item.UTILTYPE == EUMTYPE.ELEC)
                                {
                                    if (item.MointoringIdx.Contains("7"))
                                    {
                                        pow = Convert.ToInt32(row[9]);
                                        this.Variables[$"{item.EQPID}:V_PC_EM_{measCnt + 1:D2}_0007_{servCnt:D2}"].AsInteger = pow;
                                    }

                                    if (item.MointoringIdx.Contains("8"))
                                    {
                                        watt = Convert.ToInt32(row[10]);
                                        this.Variables[$"{item.EQPID}:V_PC_EM_{measCnt + 1:D2}_0008_{servCnt:D2}"].AsInteger = watt;
                                    }

                                    if (item.MointoringIdx.Contains("9"))
                                    {
                                        fact = Convert.ToInt32(row[11]);
                                        this.Variables[$"{item.EQPID}:V_PC_EM_{measCnt + 1:D2}_0009_{servCnt:D2}"].AsInteger = fact;
                                    }
                                }
                                else if (item.UTILTYPE == EUMTYPE.FLOW)
                                {
                                    INST = Convert.ToInt32(row[3]);
                                    this.Variables[$"{item.EQPID}:V_PC_FW_{measCnt + 1:D2}_0001_{servCnt:D2}"].AsInteger = INST;

                                    INTG = Convert.ToInt32(row[4]);
                                    this.Variables[$"{item.EQPID}:V_PC_FW_{measCnt + 1:D2}_0002_{servCnt:D2}"].AsInteger = INTG;
                                }

                                if (this.UTIL_INFO_PC__V_IS_UTILLOG_USE)
                                {
                                    if (item.UTILTYPE == EUMTYPE.ELEC)
                                        this.Log(Level.Verbose, $"{item.EQPID} - {item.UTILTYPE}:{measCnt} : Power = {(float)pow / 1000:f3}kW, Watt = {watt}kWh, Factor = {fact}%", this.EQP_INFO__V_W_EQP_ID, item.EQPID);
                                    else
                                        this.Log(Level.Verbose, $"{item.EQPID} - {item.UTILTYPE}:{measCnt} : INST - {INST}l, INTG - {INTG}l/min", this.EQP_INFO__V_W_EQP_ID, item.EQPID);
                                }

                                //EUMSolace.SendQueue(this.BIZINFO__V_REQQUEUE_NAME, "AAAAAAA");

                                Wait(100);
                            }
                        }
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    this.Log(Level.Debug, $"UTIL PC DATA COLLECT Exception : {ex.ToString()}", this.EQP_INFO__V_W_EQP_ID, this.EQP_INFO__V_W_EQP_ID);
                }
            }
        }
        #endregion

        #region ETC Method
        // EIF DB에 UTIL Table에서 기준 정보를 Load함
        private void LoadUtilInfo(int lineID)
        {
            try
            {
                #region EUM 기준 정보 DB 조회
                DataSet ds = new DataSet();

                string strSQL = string.Format($"SELECT * FROM TB_UTIL_INFO WHERE UTIL_USE_YN = 'Y' AND LINEID = '{lineID}' ORDER BY EQPTID");

                SqlConnection con = new SqlConnection();
                using (CDataManager mgr = new CDataManager())
                {
                    mgr.GetDataSet(ds, "TB_UTIL_INFO", strSQL);
                }

                for (int i = 0; i < ds.Tables["TB_UTIL_INFO"].Rows.Count; i++)
                {
                    CEUMInfo utilInfo = new CEUMInfo();
                    utilInfo.EQPID = ds.Tables["TB_UTIL_INFO"].Rows[i]["EQPTID"].ToString().Trim();
                    utilInfo.UTILTYPE = ds.Tables["TB_UTIL_INFO"].Rows[i]["UTILTYPE"].ToString().Trim();
                    utilInfo.EQPTYPE = ds.Tables["TB_UTIL_INFO"].Rows[i]["EQPTYPE"].ToString().Trim();
                    utilInfo.UTIL_USE_YN = ds.Tables["TB_UTIL_INFO"].Rows[i]["UTIL_USE_YN"].ToString().Trim();
                    utilInfo.UTIL_MEAS_CNT = Convert.ToInt16(ds.Tables["TB_UTIL_INFO"].Rows[i]["UTIL_MEAS_CNT"]);
                    utilInfo.UTIL_PARA_CNT = Convert.ToInt16(ds.Tables["TB_UTIL_INFO"].Rows[i]["UTIL_PARA_CNT"]);
                    utilInfo.UTIL_SERV_CNT = Convert.ToInt16(ds.Tables["TB_UTIL_INFO"].Rows[i]["UTIL_SERV_CNT"]);
                    utilInfo.UTIL_PARA_USE_IDX = ds.Tables["TB_UTIL_INFO"].Rows[i]["UTIL_PARA_USE_IDX"].ToString().Trim();

                    if (utilInfo.UTIL_PARA_USE_IDX == string.Empty || utilInfo.UTIL_PARA_USE_IDX == null) // 기준정보 IDX가 없다면 모든 ParaCount를 가져옴.
                    {
                        for (int j = 0; j < utilInfo.UTIL_PARA_CNT; j++)
                        {

                            if (j == utilInfo.UTIL_PARA_CNT - 1)
                            {
                                utilInfo.UTIL_PARA_USE_IDX += Convert.ToString(j + 1);
                            }
                            else
                            {
                                utilInfo.UTIL_PARA_USE_IDX += Convert.ToString(j + 1) + ",";
                            }
                        }
                    }

                    if (utilInfo.EQPTYPE == "PC") this.PcUtilList.Add(utilInfo);
                    if (utilInfo.EQPTYPE == "PLC") this.PlcUtilList.Add(utilInfo);
                }
                #endregion
            }
            catch (Exception ex)
            {
                this.Log(Level.Debug, $"UTIL INFO Exception : {ex.ToString()}");
            }
        }
        #endregion
    }
}