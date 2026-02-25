using System.Collections.Generic;
using System.Linq;
using System.Data;

using LGCNS.ezControl.Common;
using LGCNS.ezControl.Core;

namespace ESHG.EIF.FORM.EOLPACKER
{
    public partial class CPACKERUNIT : CElement
    {
        private const int MAX_HOST_ALARM_MSG = 8;

        public List<CReportInfo> ReportList = new List<CReportInfo>();

        protected Dictionary<string, int> dicRptPstnCode = new Dictionary<string, int>();

        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            #region SYSTEM INFO
            __INTERNAL_VARIABLE_BOOLEAN("V_SYSTEM_DATA_TIME_SET_LOCAL_REMOTE", "BASICINFO", enumAccessType.Virtual, false, true, false, string.Empty, "System Datatime set local or remote - local(true), remote(false)");
            __INTERNAL_VARIABLE_BOOLEAN("V_SET_LOCAL_DATE_TIME", "BASICINFO", enumAccessType.Virtual, false, true, false, string.Empty, "Set Local date time to equipment");

            // 20180115 Host Alarm Function Change
            __INTERNAL_VARIABLE_INTEGER("V_HOSTALARM_LINE_CNT", "ADDINFO", enumAccessType.Virtual, 0, 0, false, false, 0, string.Empty, "Host Alarm Display Line Count");
            __INTERNAL_VARIABLE_INTEGER("V_HOSTALARM_CHAR_CNT", "ADDINFO", enumAccessType.Virtual, 0, 0, false, false, 24, string.Empty, "Host Alarm Display One Line Char Count");
            __INTERNAL_VARIABLE_INTEGER("V_HOSTALARM_BYTE_SIZE", "ADDINFO", enumAccessType.Virtual, 0, 0, false, false, 24, string.Empty, "Host Alarm Display One Line Byte Size");

            __INTERNAL_VARIABLE_BOOLEAN("O_B_RELOAD_VARIABLE", "ADDINFO", enumAccessType.Out, true, true, false, string.Empty, "Reload Variable Value");

            __INTERNAL_VARIABLE_BOOLEAN("V_USE_IT_BYPASS", "ADDINFO", enumAccessType.Virtual, true, true, false, string.Empty, "Use IT Bypass, Only for New Line");

            // 20180305 오창14, CWA, NJ ESS는 설비별 Class내의 HOST_ALARM_MSG_SND Category를 사용하면 됨 이후 라인부터는 아래 Variable 사용할 것
            __INTERNAL_VARIABLE_STRING("O_W_HOST_ALARM_MSG", "HOST_ALARM_MSG_SEND", enumAccessType.Out, false, true, string.Empty, string.Empty, "Host Alarm Msg");
            __INTERNAL_VARIABLE_STRING("O_W_HOST_ALARM_MSG_01", "HOST_ALARM_MSG_SEND", enumAccessType.Out, false, true, string.Empty, string.Empty, "Host Alarm Msg 01");
            __INTERNAL_VARIABLE_STRING("O_W_HOST_ALARM_MSG_02", "HOST_ALARM_MSG_SEND", enumAccessType.Out, false, true, string.Empty, string.Empty, "Host Alarm Msg 02");
            __INTERNAL_VARIABLE_STRING("O_W_HOST_ALARM_MSG_03", "HOST_ALARM_MSG_SEND", enumAccessType.Out, false, true, string.Empty, string.Empty, "Host Alarm Msg 03");
            __INTERNAL_VARIABLE_STRING("O_W_HOST_ALARM_MSG_04", "HOST_ALARM_MSG_SEND", enumAccessType.Out, false, true, string.Empty, string.Empty, "Host Alarm Msg 04");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_PROC_STOP_TYPE", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, false, true, 0, string.Empty, "EQP Processing Stop Type");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_ACTION", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, false, true, 0, string.Empty, "Host Alarm Action");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_DISP_TYPE", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, true, false, 0, string.Empty, "Host Alarm Display Type");
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_ALARM_MSG_SEND", "HOST_ALARM_MSG_SEND", enumAccessType.Out, false, true, false, string.Empty, "Host Alarm Msg Send");

            // 2021.05.11 Add ECS Monitoring Data 
            __INTERNAL_VARIABLE_STRING("O_W_PROCESSING_EQP_ID", "ECS_MONITORING_DATA", enumAccessType.InOut, true, true, string.Empty, string.Empty, "Processing EQP ID for ECS");
            __INTERNAL_VARIABLE_STRING("O_W_EMS_MACHINE_ID", "ECS_MONITORING_DATA", enumAccessType.InOut, true, true, string.Empty, string.Empty, "EMS Machine ID for ECS");

            __INTERNAL_VARIABLE_BOOLEAN("V_USE_AGV_CALL", "ADDINFO", enumAccessType.Virtual, true, true, false, string.Empty, "Use AGV Process Function Call");
            __INTERNAL_VARIABLE_STRING("V_PORT_AGV_INFO", "ADDINFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "Port-AGV Linked Information(PORTID1:AGVID1;PORTID2:AGVID1)");
            #endregion

            #region BASIC INFO
            __INTERNAL_VARIABLE_BOOLEAN("V_DRIVER_CONNECTED", string.Empty, enumAccessType.Virtual, true, true, false, string.Empty, "Driver Connection Check");
            __INTERNAL_VARIABLE_INTEGER("V_COM_CHECK_INTERVAL", "BASICINFO", enumAccessType.Virtual, 0, 0, false, false, 9000, string.Empty, "Communication Check Interval");
            __INTERNAL_VARIABLE_STRING("V_TIME_DATA_SEND_TIME", "BASICINFO", enumAccessType.Virtual, false, false, "5", string.Empty, "Time Data Send Time");
            __INTERNAL_VARIABLE_INTEGER("V_HOST_ALARM_MSG_INTERVAL", "BASICINFO", enumAccessType.Virtual, 0, 0, false, false, 10000, string.Empty, "Host Alarm Message Interval");
            __INTERNAL_VARIABLE_INTEGER("V_WIP_DATA_RPT_INTERVAL", "BASICINFO", enumAccessType.Virtual, 0, 0, false, false, 60000, string.Empty, "WIP Data Report Interval");
            __INTERNAL_VARIABLE_INTEGER("V_EQP_RWT_RPT_INTERVAL", "BASICINFO", enumAccessType.Virtual, 0, 0, false, false, 60000, string.Empty, "EQP RWT REPORT INTERVAL");
            __INTERNAL_VARIABLE_INTEGER("V_EQP_UBM_CNT_RPT_INTERVAL", "BASICINFO", enumAccessType.Virtual, 0, 0, false, false, 60000, string.Empty, "EQP UBM CNT REPORT INTERVAL");
            __INTERNAL_VARIABLE_INTEGER("V_CURRENT_LOT_INFO_INTERVAL", "BASICINFO", enumAccessType.Virtual, 0, 0, false, false, 60000, string.Empty, "Get Current Lot Info Interval");
            __INTERNAL_VARIABLE_STRING("V_EQP_MACHINE_ID_01", "BASICINFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "Machine Equipment ID");
            __INTERNAL_VARIABLE_STRING("V_DEVICE_TYPE", "BASICINFO", enumAccessType.Virtual, false, false, string.Empty, string.Empty, "Device TYPE");

            __INTERNAL_VARIABLE_BOOLEAN("V_USE_UBM_BYPASS", "ADDINFO", enumAccessType.Virtual, true, true, false, string.Empty, "Use UBM Bypass");
            __INTERNAL_VARIABLE_BOOLEAN("V_APD_LOGGING", "BASICINFO", enumAccessType.Virtual, false, true, false, string.Empty, string.Empty);
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_ALMLOG_USE", "BASICINFO", enumAccessType.Virtual, false, true, false, string.Empty, "True - Alarm Set/Rest EIF, Biz Log 남김, False - Log 사용 안함");    //$ 2023.05.19 : Alarm Set/Rest Log 사용 유무
            __INTERNAL_VARIABLE_INTEGER("V_APD_STORAGE_PERIOD", "BASICINFO", enumAccessType.Virtual, 0, 0, false, false, 5, string.Empty, string.Empty);
            #endregion

            #region ADD INFO
            __INTERNAL_VARIABLE_BOOLEAN("V_DRY_RUN", "ADDINFO", enumAccessType.Virtual, false, true, false, string.Empty, "DRY RUN TEST");
            __INTERNAL_VARIABLE_BOOLEAN("V_DRY_RUN_OK", "ADDINFO", enumAccessType.Virtual, false, true, false, string.Empty, "DRY RUN TEST OK");
            __INTERNAL_VARIABLE_BOOLEAN("V_DRY_RUN_DIRECT_INPUT", "ADDINFO", enumAccessType.Virtual, false, true, false, string.Empty, "DRY RUN Direct input");
            __INTERNAL_VARIABLE_BOOLEAN("V_DRY_RUN_TIMEOUT", "ADDINFO", enumAccessType.Virtual, false, true, false, string.Empty, "DRY RUN Time Out");
            __INTERNAL_VARIABLE_BOOLEAN("V_DRY_RUN_NGTIMEOUT", "ADDINFO", enumAccessType.Virtual, false, true, false, string.Empty, "DRY RUN NG->Timeout Once Occur");  //$ 2024.02.29 : 사전검수용 모드 추가(NG->Timeout->OK)
            __INTERNAL_VARIABLE_BOOLEAN("V_DRY_RUN_BIZ_CONNECT", "ADDINFO", enumAccessType.Virtual, false, true, false, string.Empty, "DRY RUN Biz Connection");
            __INTERNAL_VARIABLE_SHORT("V_EQPT_TYPE", "ADDINFO", enumAccessType.Virtual, 0, 0, false, false, 1, string.Empty, "Vision=1, Packer=2");
            #endregion

            #region 7.1.1.1 [C1-1] EQP Communication Check
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_COMM_CHK", CCategory.EQP_COMM_CHK, enumAccessType.In, 0, 0, true, false, 0, "", "EQP Comm Check");
            #endregion

            #region 7.1.1.2 [C1-2] Host Communication Check 
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_COMM_CHK", CCategory.HOST_COMM_CHK, enumAccessType.Out, false, false, false, "", "Host Comm Check");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_HOST_COMM_CHK_CONF", CCategory.HOST_COMM_CHK, enumAccessType.In, true, false, false, "", "Host Comm Check Confirm");
            #endregion

            #region 7.1.1.3 [C1-3] Communication State Change Report
            __INTERNAL_VARIABLE_BOOLEAN("I_B_COMM_ON", CCategory.COMM_STAT_CHG_RPT, enumAccessType.In, true, true, false, "", "COMM ON");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_COMM_OFF", CCategory.COMM_STAT_CHG_RPT, enumAccessType.In, true, true, false, "", "COMM OFF");
            #endregion

            #region 7.1.1.4 [C1-4] Date and Time Set Request
            __INTERNAL_VARIABLE_BOOLEAN("O_B_DATE_TIME_SET_REQ", CCategory.DATE_TIME_SET_REQ, enumAccessType.Out, false, true, false, "", "Date Time Set Request");
            __INTERNAL_VARIABLE_SHORTLIST("O_W_DATE_TIME", CCategory.DATE_TIME_SET_REQ, enumAccessType.Out, 6, 0, 0, false, true, 0, "", "Date and Time");
            #endregion

            #region 7.1.2.1 [C2-1] Equipment State Change Report
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Equipment State");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SUBSTAT", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Equipment SubState");

            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_POPUP_DELAY_TIME", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "User Stop Pop-up Delay Time");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_LOT_RUNNING", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, true, true, false, "", "Lot Running");
            __INTERNAL_VARIABLE_STRING("I_W_CURRENT_LOT_ID", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, true, true, string.Empty, "", "Current Lot ID");
            #endregion

            #region 7.1.2.2 [C2-2] Alarm Report
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Equipment State");
            #endregion

            #region 7.1.2.3 [C2-3] Host Alarm Message Send
            for (int i = 5; i <= MAX_HOST_ALARM_MSG; i++)
            {
                __INTERNAL_VARIABLE_STRING($"O_W_HOST_ALARM_MSG_{i:D2}", "HOST_ALARM_MSG_SEND", enumAccessType.Out, false, true, string.Empty, string.Empty, $"Host Alarm Msg {i:D2}");
            }
            #endregion

            #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report
            __INTERNAL_VARIABLE_BOOLEAN("I_B_AUTO_MODE", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, false, "", "Auto Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_IT_BYPASS", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, false, "", "IT Bypass");
            __INTERNAL_VARIABLE_SHORT("I_W_HMI_LANG_TYPE", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "HMI Language Type");

            __INTERNAL_VARIABLE_STRING("I_W_MODEL_ID", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, string.Empty, string.Empty, "Model ID");
            __INTERNAL_VARIABLE_STRING("I_W_LINE_ID", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, string.Empty, string.Empty, "Line ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_FIRST_TRAY_USE", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, false, string.Empty, "First Tray Use");
            #endregion

            #region 7.1.2.6 [C2-6] Remote Command Send
            __INTERNAL_VARIABLE_BOOLEAN("O_B_REMOTE_COMMAND_SEND", CCategory.REMOTE_COMM_SND, enumAccessType.Out, false, true, false, "", "Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("O_W_REMOTE_COMMAND_CODE", CCategory.REMOTE_COMM_SND, enumAccessType.Out, 0, 0, false, true, 0, "", "Remote Command Code");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REMOTE_COMMAND_CONF", CCategory.REMOTE_COMM_SND, enumAccessType.In, true, true, false, "", "Remote Command Confirm");
            __INTERNAL_VARIABLE_SHORT("I_W_REMOTE_COMMAND_CONF_ACK", CCategory.REMOTE_COMM_SND, enumAccessType.In, 0, 0, false, true, 0, "", "Remote Command Confirm ACK");
            #endregion

            #region 7.1.2.8 [C2-8] Alarm Set Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_SET_CONF", CCategory.ALARM_RPT, enumAccessType.Out, false, true, false, "", "Alarm Set Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_SET_REQ", CCategory.ALARM_RPT, enumAccessType.In, true, true, false, "", "Alarm Set Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_SET_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, false, true, 0, "", "Alarm Set ID");
            #endregion

            #region 7.1.2.9 [C2-9] Alarm Reset Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_RESET_CONF", CCategory.ALARM_RPT, enumAccessType.Out, false, true, false, "", "Alarm Reset Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_RESET_REQ", CCategory.ALARM_RPT, enumAccessType.In, true, true, false, "", "Alarm Reset Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_RESET_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, false, true, 0, "", "Alarm Reset ID");
            #endregion

            #region 7.1.2.10 [C2-10] Smoke Alarm Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_EQP_SMOKE_STATUS_CONF", CCategory.SMOKE_RPT, enumAccessType.Out, false, true, false, "", "Eqp Smoke Dectect Request");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_SMOKE_DETECT_REQ", CCategory.SMOKE_RPT, enumAccessType.In, true, true, false, "", "Eqp Smoke Status");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SMOKE_STATUS", CCategory.SMOKE_RPT, enumAccessType.In, 0, 0, false, true, 0, "", "Eqp Smoke Detect Confirm");
            #endregion

            #region 동적생성 관련
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.PALLET_ID_RPT, $"{CCategory.CARR_ID_RPT}_01", "Pallet ID Reading", "01"));
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.TRAY_ID_RPT, $"{CCategory.CARR_ID_RPT}_02", "Carrier(Tray) ID Reading", "01"));

            ReportList.Add(new CReportInfo((int)REPORT_TYPE.PALLET_JOB_START_RPT, $"{CCategory.CARR_JOB_START}_01", "Pallet job Start", "01"));
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.TRAY_JOB_START_RPT, $"{CCategory.CARR_JOB_START}_02", "Carrier(Tray) Job Start", "01"));

            ReportList.Add(new CReportInfo((int)REPORT_TYPE.PALLET_OUT_RPT, $"{CCategory.CARR_OUT_RPT}", "Pallet Out", "01"));

            ReportList.Add(new CReportInfo((int)REPORT_TYPE.TRAY_END_PACKING, $"{CCategory.CARR_STAT_CHG}_01", "Carrier(Tray) End Packing", "01"));
            //lstRpt.Add(new CReportInfo((int)REPORT_TYPE.PALLET_CHECK_CONFIRM, $"{CCategory.CARR_STAT_CHG}_02", "Pallet Check Confirm", "01")); //사용안함

            ReportList.Add(new CReportInfo((int)REPORT_TYPE.PALLET_JOB_END_RPT, $"{CCategory.CARR_JOB_END}", "Pallet Job End", "01"));

            //lstRpt.Add(new CReportInfo((int)REPORT_TYPE.APD_RPT, $"{CCategory.APD_RPT}", "Cell Vision Inspection Result", "10")); //사용안함
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.APD_RPT, $"{CCategory.APD_RPT}_01", "Cell Vision Inspection Result 1", "01"));
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.APD_RPT, $"{CCategory.APD_RPT}_02", "Cell Vision Inspection Result 2", "01"));

            //lstRpt.Add(new CReportInfo((int)REPORT_TYPE.CELL_ID_CONF_REQ, $"{CCategory.CELL_ID_CONF_REQ}", "Cell Input #1,2 ID Reading", "10"));
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.CELL_ID_CONF_REQ, $"{CCategory.CELL_ID_CONF_REQ}_01", "Cell Input #3 ID Reading", "01"));
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.CELL_ID_CONF_REQ, $"{CCategory.CELL_ID_CONF_REQ}_02", "Cell Input #4 ID Reading", "01"));

            ReportList.Add(new CReportInfo((int)REPORT_TYPE.CELL_OUT_NG, $"{CCategory.CELL_OUT_RPT}_01", "NG Port #1", "01"));
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.CELL_OUT_NG, $"{CCategory.CELL_OUT_RPT}_02", "NG Port #2", "01"));

            //lstRpt.Add(new CReportInfo((int)REPORT_TYPE.CELL_INFO_REQ, $"{CCategory.CELL_INFO_REQ}_01", "Cell Information Request #1", "01")); //사용안함
            //lstRpt.Add(new CReportInfo((int)REPORT_TYPE.CELL_INFO_REQ, $"{CCategory.CELL_INFO_REQ}_02", "Cell Information Request #2", "01")); //사용안함

            ReportList.Add(new CReportInfo((int)REPORT_TYPE.MODEL_ID_CHG, $"{CCategory.PROC_PARA_CHG_RPT}_01", "Model ID Change", "01"));
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.LINE_ID_CHG, $"{CCategory.PROC_PARA_CHG_RPT}_02", "Line ID Change", "01"));
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.FIRST_TRAY_USE_CHG, $"{CCategory.PROC_PARA_CHG_RPT}_03", "First Tray Use State Change", "01"));

            //lstRpt.Add(new CReportInfo((int)REPORT_TYPE.PALET_INFO_REQ, $"{CCategory.PROC_PARA_REQ}_01", "RFID Reading Fail", "01")); //사용안함

            ReportList.Add(new CReportInfo((int)REPORT_TYPE.LR_PORT_STAT_CHG, $"{CCategory.PORT_STAT_CHG}_01", "Port #101 State", "101"));
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.UR_PORT_STAT_CHG, $"{CCategory.PORT_STAT_CHG}_02", "Port #102 State", "101"));

            // JH 2025.06.13 재료 교체 알람 추가 [UC2 내역 반영]
            ReportList.Add(new CReportInfo((int)REPORT_TYPE.MTRL_STAT_CHG_RPT, $"{CCategory.MTRL_STAT_CHG}", "Material State Change Event Code", "01"));

            dicRptPstnCode = ReportList.Where(x => x._PstnNo > -1).ToDictionary(x => x._CategoryName, x => x._PstnNo);
            #endregion

            #region ADD INFO
            __INTERNAL_VARIABLE_INTEGER("V_PACKING_TRAY_COUNT", "BASICINFO", enumAccessType.Virtual, 0, 0, false, false, 0, string.Empty, "Packing Tray Count");  //$ 2022.05.20
            __INTERNAL_VARIABLE_INTEGER("V_PACKING_CELL_COUNT", "BASICINFO", enumAccessType.Virtual, 0, 0, false, false, 0, string.Empty, "Packing Cell Count");  //$ 2022.05.20   

            __INTERNAL_VARIABLE_INTEGER("V_CELL_CLCT_DATA_CNT", "ADDINFO", enumAccessType.Virtual, 0, 0, false, false, 120, string.Empty, "1 Cell's CLCT Data Count");  //$ 2022.08.09 : 2D 치수 불량 관련 APD 신규 항목 추가 // JH 2024.01.25 113 ->120 수정

            __INTERNAL_VARIABLE_INTEGER("V_WAIT_TIME", "ADDINFO", enumAccessType.Virtual, 0, 0, false, false, 0, string.Empty, "0: No Wait, ms");  //$ 2022.10.20 : APD <-> CellID보고시 Wait 주기 ms 단위

            __INTERNAL_VARIABLE_INTEGER("V_UTILITY_SCAN_INTERVAL", "UTILIT_INFO", enumAccessType.Virtual, 0, 0, false, true, 60, "", "UTILITY DATA COLLECT INTERVAL (SEC)");    //2023.10.19 : 전력량계 수집 주기
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_UTILITY_LOG_USE", "UTILIT_INFO", enumAccessType.Virtual, true, false, false, "", "True - LOG [O], False - LOG [X]");
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_UTILITY_USE_FLAG", "UTILIT_INFO", enumAccessType.Virtual, true, false, true, "", "True - USE [O], False - USE [X]");

            __INTERNAL_VARIABLE_INTEGER("V_W_USE_TIME_SYNC", "SYSTEM", enumAccessType.Virtual, 24, 0, true, false, 0, string.Empty, "Data SYNC Time");  //2023.11.16 LWY : ESGM2 개발자 설정 시간동기화 추가 Virtual Var로 관리, 시간만 입력 가능 ( 1 ~ 23 )
            #endregion

            #region 7.2.3.6 [G3-6] WIP Data Report
            __INTERNAL_VARIABLE_SHORT("I_W_TACT_TIME", CCategory.WIP_DATA_RPT, enumAccessType.In, 0, 0, true, false, 0, string.Empty, "Tact Time");
            #endregion

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.PORT_STAT_REFRESH_REQ, enumAccessType.In, true, true, false, string.Empty, "Port State Refresh Request");

            foreach (CReportInfo info in ReportList)
            {
                switch (info._Type)
                {
                    // JH 2025.06.13 재료 교체 알람 추가 [UC2 내역 반영]
                    #region 7.2.1.1 [G1-1] Material Monitoring Data Report
                    case (int)REPORT_TYPE.MTRL_STAT_CHG_RPT:
                        __INTERNAL_VARIABLE_STRING($"{CTag.I_W_TRIGGER_}MTRL_STAT_CHG_EVENT_CODE", info._CategoryName, enumAccessType.In, true, true, string.Empty, string.Empty, "Material State Change Event Code");
                        break;

                    #endregion

                    #region 7.2.2.1 [G2-1] Carrier ID Report
                    case (int)REPORT_TYPE.PALLET_ID_RPT:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");
                        __INTERNAL_VARIABLE_STRING("O_W_PALLET_LOT_ID", info._CategoryName, enumAccessType.Out, false, true, string.Empty, string.Empty, "Pallet Lot ID");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet ID");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_RFID_USING", info._CategoryName, enumAccessType.In, false, true, false, string.Empty, "RFID Using");
                        __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, "RFID Reading Type");
                        __INTERNAL_VARIABLE_SHORT("I_W_PALLET_INPUT_TYPE", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, "Pallet Input Type");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_PALLET_RFID_PASS_MODE", info._CategoryName, enumAccessType.In, false, true, false, string.Empty, "Pallet RFID Pass Mode");
                        break;

                    case (int)REPORT_TYPE.TRAY_ID_RPT:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");
                        __INTERNAL_VARIABLE_STRING("O_W_HOST_TRAY_ID", info._CategoryName, enumAccessType.Out, false, true, string.Empty, string.Empty, "Carrier Lot ID");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Tray ID");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_RFID_USING", info._CategoryName, enumAccessType.In, false, true, false, string.Empty, "RFID Using");
                        __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, "RFID Reading Type");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_RFID_PASS_MODE", info._CategoryName, enumAccessType.In, false, true, false, string.Empty, "Tray RFID Pass Mode");
                        break;
                    #endregion

                    #region 7.2.2.2 [G2-2] Carrier Job Start Report
                    case (int)REPORT_TYPE.PALLET_JOB_START_RPT:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");
                        __INTERNAL_VARIABLE_STRING("O_W_HOST_TRAY_ID", info._CategoryName, enumAccessType.Out, false, true, string.Empty, string.Empty, "Host Tray ID");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet ID");
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_LOT_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet-Lot ID");
                        __INTERNAL_VARIABLE_SHORT("I_W_PALLET_OUTPUT_TYPE", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, "Pallet Output Type");
                        break;

                    case (int)REPORT_TYPE.TRAY_JOB_START_RPT:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet ID");
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_LOT_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet-Lot ID");
                        __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Tray ID");
                        __INTERNAL_VARIABLE_SHORT("I_W_TRAY_SEQ_NO", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, "ray Sequence Number");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_MANUAL_OUTPUT_STAT", info._CategoryName, enumAccessType.In, false, true, false, string.Empty, "Tray Manual Output State");
                        break;
                    #endregion

                    #region 7.2.2.3 [G2-3] Carrier Output Report
                    case (int)REPORT_TYPE.PALLET_OUT_RPT:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet ID");
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_LOT_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet-Lot ID");
                        __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_TYPE", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, $"Output Type");
                        break;

                    #endregion

                    #region 7.2.2.4 [G2-4] Carrier State Change Report
                    case (int)REPORT_TYPE.TRAY_END_PACKING:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet ID");
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_LOT_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet-Lot ID");
                        __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Tray ID");
                        __INTERNAL_VARIABLE_SHORT("I_W_TRAY_SEQ_NO", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, "ray Sequence Number");
                        __INTERNAL_VARIABLE_SHORT("I_W_TRAY_CELL_CNT", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, "Tray Cell Qty");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EMPTY_STAT", info._CategoryName, enumAccessType.In, false, true, false, string.Empty, "Output Tray Empty State");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_MANUAL_OUTPUT_STAT", info._CategoryName, enumAccessType.In, false, true, false, string.Empty, "Tray Manual Output State");

                        for (int i = 1; i <= 20; i++)
                        {
                            __INTERNAL_VARIABLE_STRING($"I_W_CELL_ID_{i:D2}", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, $"Location #{i:D2} Cell ID");
                        }
                        break;

                    case (int)REPORT_TYPE.PALLET_CHECK_CONFIRM:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet ID");
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_LOT_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet-Lot ID");
                        __INTERNAL_VARIABLE_SHORT("I_W_TOTAL_TRAY_CNT", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, $"Total Tray Qty");
                        __INTERNAL_VARIABLE_SHORT("I_W_TOTAL_CELL_CNT", info._CategoryName, enumAccessType.In, 0, 0, true, false, 0, string.Empty, $"Total Cell Qty");
                        __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_TYPE", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, $"Output Type");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_PALLET_EMPTY_STAT", info._CategoryName, enumAccessType.In, false, true, false, string.Empty, "Pallet Empty State");
                        break;
                    #endregion

                    #region 7.2.2.6 [G2-6] Carrier Job End Report
                    case (int)REPORT_TYPE.PALLET_JOB_END_RPT:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet ID");
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_LOT_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet-Lot ID");
                        __INTERNAL_VARIABLE_SHORT("I_W_TOTAL_TRAY_CNT", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, $"Total Tray Qty");
                        __INTERNAL_VARIABLE_SHORT("I_W_TOTAL_CELL_CNT", info._CategoryName, enumAccessType.In, 0, 0, true, false, 0, string.Empty, $"Total Cell Qty");
                        __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_TYPE", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, $"Output Type");
                        __INTERNAL_VARIABLE_STRING("I_W_TRAY_ORIGINAL_PALLET_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Tray Original Pallet ID");
                        break;

                    #endregion

                    #region 7.2.3.5 [G3-5] Actual Processing Data Report
                    case (int)REPORT_TYPE.APD_RPT:

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, "Processing Result Data Confirm");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, "Processing Result Data Repor");
                        __INTERNAL_VARIABLE_STRING($"I_W_CELL_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, $"Data Report Cell ID");

                        #region APD List
                        __INTERNAL_VARIABLE_SHORTLIST($"APD_CELL_DATA_001", info._CategoryName, enumAccessType.In, 6, 0, 0, true, true, 0, string.Empty, "검사시간");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_002", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "검사설비");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_003", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "Cell ID");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_004", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "검사 Model");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_005", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "투입 Type");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_006", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "검사 Mode");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_007", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "검사 Type");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_008", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "Rework SEQ");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_009", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "치수 Judge");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_010", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "외관 Judge");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_011", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "Manual Judge");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_012", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "Review Judge");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_013", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "Reason1");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_014", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "Reason2");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_015", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "Position");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_016", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "Width");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_017", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "Length");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_018", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "X");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_019", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "Y");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_020", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[W_1] Cell Width(Cathode)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_021", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[W_2] Cell Width");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_022", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[W_3] Cell Width");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_023", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[W_4] Cell Width");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_024", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[W_5] Cell Width");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_025", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[W_6] Cell Width");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_026", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[W_7] Cell Width(Anode)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_027", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[L_1] Cell Length_PKG");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_028", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[L_2] Cell Length_DGS");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_029", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[FF_1] Film to Film_PKG");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_030", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[FF_2] Film to Film_DGS");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_031", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LL] Lead to Lead");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_032", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LP_1] Lead Position_PKG(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_033", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LP_2] Lead Position_PKG(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_034", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[FP_1] Lead Film Protrusion_PKG(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_035", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[FP_2] Lead Film Protrusion_DGS(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_036", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[FP_3] Lead Film Protrusion_PKG(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_037", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[FP_4] Lead Film Protrusion_DGS(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_038", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[FW_1] Lead Film Width(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_039", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[FW_2] Lead Film Width(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_040", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LFW_1] Lead to Lead Film Width_PKG(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_041", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LFW_2] Lead to Lead Film Width_DGS(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_042", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LFW_3] Lead to Lead Film Width_PKG(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_043", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LFW_4] Lead to Lead Film Width_DGS(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_044", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LPC_1] Lead to Pouch _PKG(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_045", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LPC_2] Lead to Pouch _DGS(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_046", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LPC_3] Lead to Pouch _PKG(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_047", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LPC_4] Lead to Pouch _DGS(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_048", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[CCL_1] Corner Cutting Length_PKG(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_049", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[CCL_2] Corner Cutting Length_DGS(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_050", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[CCL_3] Corner Cutting Length_PKG(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_051", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[CCL_4] Corner Cutting Length_DGS(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_052", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[CCD_1] Corner Cutting Depth_PKG(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_053", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[CCD_2] Corner Cutting Depth_DGS(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_054", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[CCD_3] Corner Cutting Depth_PKG(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_055", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[CCD_4] Corner Cutting Depth_DGS(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_056", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[ETP_1] Tape Position_(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_057", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[ETP_2] Tape Position_(C)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_058", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[ETP_3] Tape Position_(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_059", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[B_1] Bat ear(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_060", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[B_2] Bat ear(-)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_061", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LP_3] Lead Position(+)_DGS");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_062", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LP_4] Lead Position(-)_DGS");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_063", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[TL] Tape Length");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_064", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LW_1] Lead Width(+)");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_065", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "[LW_2] Lead Width(-)");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_066", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[W_1] Cell Width(Cathode) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_067", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[W_2] Cell Width JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_068", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[W_3] Cell Width JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_069", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[W_4] Cell Width JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_070", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[W_5] Cell Width JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_071", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[W_6] Cell Width JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_072", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[W_7] Cell Width(Anode) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_073", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[L_1] Cell Length_PKG JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_074", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[L_2] Cell Length_DGS JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_075", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[FF_1] Film to Film_PKG JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_076", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[FF_2] Film to Film_DGS JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_077", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LL] Lead to Lead JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_078", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LP_1] Lead Position_PKG(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_079", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LP_2] Lead Position_PKG(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_080", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[FP_1] Lead Film Protrusion_PKG(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_081", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[FP_2] Lead Film Protrusion_DGS(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_082", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[FP_3] Lead Film Protrusion_PKG(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_083", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[FP_4] Lead Film Protrusion_DGS(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_084", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[FW_1] Lead Film Width(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_085", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[FW_2] Lead Film Width(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_086", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LFW_1] Lead to Lead Film Width_PKG(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_087", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LFW_2] Lead to Lead Film Width_DGS(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_088", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LFW_3] Lead to Lead Film Width_PKG(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_089", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LFW_4] Lead to Lead Film Width_DGS(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_090", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LPC_1] Lead to Pouch _PKG(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_091", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LPC_2] Lead to Pouch _DGS(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_092", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LPC_3] Lead to Pouch _PKG(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_093", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LPC_4] Lead to Pouch _DGS(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_094", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[CCL_1] Corner Cutting Length_PKG(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_095", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[CCL_2] Corner Cutting Length_DGS(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_096", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[CCL_3] Corner Cutting Length_PKG(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_097", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[CCL_4] Corner Cutting Length_DGS(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_098", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[CCD_1] Corner Cutting Depth_PKG(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_099", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[CCD_2] Corner Cutting Depth_DGS(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_100", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[CCD_3] Corner Cutting Depth_PKG(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_101", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[CCD_4] Corner Cutting Depth_DGS(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_102", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[ETP_1] Tape Position_(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_103", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[ETP_2] Tape Position_(C) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_104", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[ETP_3] Tape Position_(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_105", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[B_1] Bat ear(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_106", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[B_2] Bat ear(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_107", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LP_3] Lead Position (+)_DGS JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_108", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LP_4] Lead Position (-)_DGS JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_109", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[TL] Tape Length JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_110", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LW_1] Lead Width(+) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_111", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "[LW_2] Lead Width(-) JUDGE");
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_112", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "2D BARCODE Size JUDGE");   //$ 2022.08.09 : 2D 치수 불량 관련 APD 신규 항목 추가
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_113", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "MES Judge");   //$ 2022.08.17 : Vision MES Judge 불량 관련 APD 신규 항목 추가
                        __INTERNAL_VARIABLE_STRING($"APD_CELL_DATA_114", info._CategoryName, enumAccessType.In, false, false, string.Empty, string.Empty, "W Depth Judge");//$ 2023.04.24 : W Depth Judge 불량 관련 APD 신규 항목 추가 
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_115", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "W Depth 1_FIRST");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_116", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "W Depth 1_SECOND");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_117", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "W Depth 2_FIRST");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_118", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "W Depth 2_SECOND");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_119", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "W Depth 3_FIRST");
                        __INTERNAL_VARIABLE_INTEGER($"APD_CELL_DATA_120", info._CategoryName, enumAccessType.In, 0, 0, false, false, 0, string.Empty, "W Depth 3_SECOND");

                        #endregion
                        break;
                    #endregion

                    #region 7.2.4.1 [G4-1] Cell ID Confirm Request
                    case (int)REPORT_TYPE.CELL_ID_CONF_REQ:

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");
                        __INTERNAL_VARIABLE_STRING("O_W_PRODUCT_ID", info._CategoryName, enumAccessType.Out, false, true, string.Empty, string.Empty, "Product ID");
                        __INTERNAL_VARIABLE_STRING("O_W_PKG_LOT_ID", info._CategoryName, enumAccessType.Out, false, true, string.Empty, string.Empty, "Package Lot ID");
                        __INTERNAL_VARIABLE_INTEGER("O_W_NG_TYPE", info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, "NG Type"); // $ 2022.11.21 Host NG Type 추가
                                                                                                                                                           //__INTERNAL_VARIABLE_STRING("O_W_WORK_TYPE", info._CategoryName, enumAccessType.Out, false, true, string.Empty, string.Empty, "Work Type");  //$ 사양에 없음

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, $"Data Report Cell ID");

                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_001", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 크랙 불량 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_002", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 부분 약실링 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_003", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 인쇄 불량 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_004", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 긁힘 찍힘 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_005", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 리드 불량 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_006", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 돌출 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_007", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 오염 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_008", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 테이핑 외관 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_009", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 윙폴딩 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_010", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 리드 필름Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_011", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 주름 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_012", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 전극 탈리 Judg");
                        __INTERNAL_VARIABLE_STRING($"I_W_VISION_JUDG_013", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Vision 외관 기타 Judg");

                        break;
                    #endregion

                    #region 7.2.4.3 [G4-3] Cell Output Report
                    case (int)REPORT_TYPE.CELL_OUT_NG:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, $"Data Report Cell ID");
                        __INTERNAL_VARIABLE_SHORT("I_W_NG_TYPE", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, $"NG Type");
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet ID");
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_LOT_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet-Lot ID");
                        break;
                    #endregion

                    #region 7.2.4.6 [G4-6] Cell Information Request
                    case (int)REPORT_TYPE.CELL_INFO_REQ:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");
                        __INTERNAL_VARIABLE_STRING("O_W_CELL_ID", info._CategoryName, enumAccessType.Out, false, true, string.Empty, string.Empty, "Information Send Cell ID_01");
                        __INTERNAL_VARIABLE_SHORT("O_W_SCRAP_RST", info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, "Cell ID_01 Scrap Result");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, $"Cell ID");
                        break;
                    #endregion

                    #region 7.3.5.1 [S5-1] Processing Parameter Change Report
                    case (int)REPORT_TYPE.MODEL_ID_CHG:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_MODEL_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Model ID");
                        break;

                    case (int)REPORT_TYPE.LINE_ID_CHG:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_LINE_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Line ID");
                        break;

                    case (int)REPORT_TYPE.FIRST_TRAY_USE_CHG:
                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_FIRST_TRAY_USE_CHG", info._CategoryName, enumAccessType.In, false, true, false, string.Empty, "First Tray Use State Change");
                        break;
                    #endregion

                    #region 7.3.5.3 [S5-3] Processing Parameter Request
                    case (int)REPORT_TYPE.PALET_INFO_REQ:

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, info._CategoryName, enumAccessType.Out, false, true, true, string.Empty, info._Comment + " Conf");
                        __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, info._Comment + " Ack");
                        __INTERNAL_VARIABLE_STRING("O_W_PALLET_LOT_ID", info._CategoryName, enumAccessType.Out, false, true, string.Empty, string.Empty, "Product ID");
                        __INTERNAL_VARIABLE_SHORT("O_W_OUTPUT_RPT_STAT", info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, "Output Report State");
                        __INTERNAL_VARIABLE_SHORT("O_W_OUTPUT_TYPE", info._CategoryName, enumAccessType.Out, 0, 0, false, true, 0, string.Empty, "Pallet Output Type");

                        __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, info._CategoryName, enumAccessType.In, true, true, false, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_STRING("I_W_PALLET_ID", info._CategoryName, enumAccessType.In, false, true, string.Empty, string.Empty, "Pallet ID");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_RFID_USING", info._CategoryName, enumAccessType.In, false, true, false, string.Empty, "RFID Using");
                        __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", info._CategoryName, enumAccessType.In, 0, 0, false, true, 0, string.Empty, "RFID Reading Type");
                        break;
                    #endregion

                    #region 7.5.1.1 [T1-1] Port State Change Report
                    case (int)REPORT_TYPE.LR_PORT_STAT_CHG:
                        __INTERNAL_VARIABLE_SHORT($"{CTag.I_W_TRIGGER_}STAT", info._CategoryName, enumAccessType.In, 0, 0, true, true, 0, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_SHORT($"{CTag.I_W_TRIGGER_}OP_MODE", info._CategoryName, enumAccessType.In, 0, 0, true, true, 0, string.Empty, "1 : Auto, 2 : Manual, 3 : Semi-Auto");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_CARR_EXIST", info._CategoryName, enumAccessType.In, true, true, false, string.Empty, "Carrier Exist");
                        __INTERNAL_VARIABLE_SHORT("I_W_TR_TYPE", info._CategoryName, enumAccessType.In, 0, 0, true, true, 0, string.Empty, "Transfer Type");
                        __INTERNAL_VARIABLE_STRING($"{CTag.I_W_TRIGGER_}CARRIER_ID", info._CategoryName, enumAccessType.In, true, true, string.Empty, string.Empty, "Pallet ID");
                        break;

                    case (int)REPORT_TYPE.UR_PORT_STAT_CHG:
                        __INTERNAL_VARIABLE_SHORT($"{CTag.I_W_TRIGGER_}STAT", info._CategoryName, enumAccessType.In, 0, 0, true, true, 0, string.Empty, info._Comment);
                        __INTERNAL_VARIABLE_SHORT($"{CTag.I_W_TRIGGER_}OP_MODE", info._CategoryName, enumAccessType.In, 0, 0, true, true, 0, string.Empty, "1 : Auto, 2 : Manual, 3 : Semi-Auto");
                        __INTERNAL_VARIABLE_BOOLEAN("I_B_CARR_EXIST", info._CategoryName, enumAccessType.In, true, true, false, string.Empty, "Carrier Exist");
                        __INTERNAL_VARIABLE_SHORT("I_W_TR_TYPE", info._CategoryName, enumAccessType.In, 0, 0, true, true, 0, string.Empty, "Transfer Type");
                        __INTERNAL_VARIABLE_STRING($"{CTag.I_W_TRIGGER_}CARRIER_ID", info._CategoryName, enumAccessType.In, true, true, string.Empty, string.Empty, "Carrier ID");
                        __INTERNAL_VARIABLE_STRING("I_W_CARRIER_LOT_ID", info._CategoryName, enumAccessType.In, true, true, string.Empty, string.Empty, "Carrier Lot ID");
                        __INTERNAL_VARIABLE_SHORT("I_W_OUT_TYPE_01", info._CategoryName, enumAccessType.In, 0, 0, true, true, 0, string.Empty, "Pallet Output Type");
                        __INTERNAL_VARIABLE_SHORT("I_W_OUT_TYPE_02", info._CategoryName, enumAccessType.In, 0, 0, true, true, 0, string.Empty, "Tray Output Type");
                        break;
                    #endregion

                    default:
                        break;
                }
            }
        }
    }
}