using System;
using LGCNS.ezControl.Common;
using LGCNS.ezControl.EIF.Solace;

using ESHG.EIF.FORM.COMMON;
using SolaceSystems.Solclient.Messaging;

namespace ESHG.EIF.FORM.EOL
{
    public partial class CEOL : CSolaceEIFServerBizRule
    {

        public IEIF_Biz EIF_Biz => (IEIF_Biz)Implement;

        #region Class Member variable
        public const string EQPTYPE = "EOL2";  //$ 2021.07.05 : Modeler Element의 Nick과 반드시 일치 시키시오!!
        #endregion

        #region FactovaLync Method Override
        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            #region Virtual Variable
            __INTERNAL_VARIABLE_STRING("V_W_EQP_ID", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "EQP ID");
            __INTERNAL_VARIABLE_INTEGER("V_TACTTIME_INTERVAL", "EQP_INFO", enumAccessType.Virtual, 0, 0, false, true, 60, "", "TactTime Interval(Sec) - 0: Not Use"); //$ 2022.08.01 : Tacttime 수집 주기  

            __INTERNAL_VARIABLE_INTEGERLIST("V_W_SYSTEM_SYNC_TIME", "SYSTEM", enumAccessType.Virtual, 4, 0, 0, false, false, 0, string.Empty, "SYSTEM Sync Time");
            __INTERNAL_VARIABLE_INTEGER("V_W_USE_TIME_SYNC", "SYSTEM", enumAccessType.Virtual, 24, 0, true, false, 0, string.Empty, "Data SYNC Time");  //2023.11.16 LWY : ESGM2 개발자 설정 시간동기화 추가 Virtual Var로 관리, 시간만 입력 가능 ( 1 ~ 23 )

            __INTERNAL_VARIABLE_SHORT("V_REMOTE_CMD", CCategory.REMOTE_COMM_SND, enumAccessType.Virtual, 0, 0, false, true, 0, "", "Remote Command Send");

            //2025.07.03 PRINT USE 모드 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_W_PRINTER_USE", "EQP_INFO", enumAccessType.Virtual, true, false, false, "", "2D PRINT USE Y/N MODE");

            __INTERNAL_VARIABLE_BOOLEAN("V_IS_NAK_TEST", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 무조건 Nak Reply, False - Nak Test 사용 안함");           //$ 2023.01.05 : Test Mode 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_TIMEOUT_TEST", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 무조건 Time Out, False - Timeout Test 사용 안함");    //$ 2023.01.05 : Test Mode 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_ALMLOG_USE", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - Alarm Set/Rest EIF, Biz Log 남김, False - Log 사용 안함");    //$ 2023.05.18 : Alarm Set/Rest Log 사용 유무           
            #endregion

            #region 7.1.1.1 [C1-1] EQP Communication Check
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_COMM_CHK", CCategory.EQP_COMM_CHK, enumAccessType.In, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=4000", "EQP Comm Check");
            #endregion

            #region 7.1.1.2 [C1-2] Host Communication Check 
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_COMM_CHK", CCategory.HOST_COMM_CHK, enumAccessType.Out, false, false, false, "DEVICE_TYPE=B, ADDRESS_NO=3000", "Host Comm Check");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_HOST_COMM_CHK_CONF", CCategory.HOST_COMM_CHK, enumAccessType.In, true, false, false, "DEVICE_TYPE=B, ADDRESS_NO=4000", "Host Comm Check Confirm");
            #endregion

            #region 7.1.1.3 [C1-3] Communication State Change Report
            __INTERNAL_VARIABLE_BOOLEAN("I_B_COMM_ON", CCategory.COMM_STAT_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4001", "COMM ON");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_COMM_OFF", CCategory.COMM_STAT_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4002", "COMM OFF");
            #endregion

            #region 7.1.1.4 [C1-4] Date and Time Set Request
            __INTERNAL_VARIABLE_BOOLEAN("O_B_DATE_TIME_SET_REQ", CCategory.DATE_TIME_SET_REQ, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3001", "Date Time Set Request");
            __INTERNAL_VARIABLE_SHORTLIST("O_W_DATE_TIME", CCategory.DATE_TIME_SET_REQ, enumAccessType.Out, 6, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3000", "Date and Time");
            #endregion

            #region 7.1.2.1 [C2-1] Equipment State Change Report
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4010", "Equipment State");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SUBSTAT", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4012", "Equipment SubState");

            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4011", "Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_POPUP_DELAY_TIME", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4013", "User Stop Pop-up Delay Time");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_TACT_TIME", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4040", "EQP Tact Time");

            __INTERNAL_VARIABLE_STRING("I_W_CURRENT_LOT_ID", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "Current Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Current Group Lot ID");
            #endregion

            #region 7.1.2.2 [C2-2] Alarm Report
            //__INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Alarm ID");
            //__INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Equipment State");
            //__INTERNAL_VARIABLE_BOOLEAN("I_B_EQP_ALARM_TYPE", CCategory.ALARM_RP, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4008", "EQP Alarm Type");
            #endregion

            #region 7.1.2.3 [C2-3] Host Alarm Message Send
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_ALARM_MSG_SEND", "HOST_ALARM_MSG_SEND", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3002", "Host Alarm Msg Send");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_SEND_SYSTEM", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C00", "Alarm Send System");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_PROC_STOP_TYPE", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C01", "EQP Processing Stop Type");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_DISP_TYPE", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C02", "Host Alarm Display Type");

            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_ALARM_ID", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C03", "Host Alarm Code");
            __INTERNAL_VARIABLE_STRINGLIST("O_W_HOST_ALARM_MSG", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 2, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=5C05,LENGTH=500, ENCODING=utf-16", "Host Alarm Message");
            #endregion

            #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report
            __INTERNAL_VARIABLE_BOOLEAN("I_B_AUTO_MODE", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4003", "Auto Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_IT_BYPASS", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4004", "IT Bypass");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REWORK_MODE", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4009", "Rework Mode");
            __INTERNAL_VARIABLE_SHORT("I_W_HMI_LANG_TYPE", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4001", "HMI Language Type");
            #endregion

            #region 7.1.2.6 [C2-6] Remote Command Send
            __INTERNAL_VARIABLE_BOOLEAN("O_B_REMOTE_COMMAND_SEND", CCategory.REMOTE_COMM_SND, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=300E", "Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("O_W_REMOTE_COMMAND_CODE", CCategory.REMOTE_COMM_SND, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=300E", "Remote Command Code");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REMOTE_COMMAND_CONF", CCategory.REMOTE_COMM_SND, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=400E", "Remote Command Confirm");
            __INTERNAL_VARIABLE_SHORT("I_W_REMOTE_COMMAND_CONF_ACK", CCategory.REMOTE_COMM_SND, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=400E", "Remote Command Confirm ACK");
            #endregion

            #region 7.1.2.8 [C2-8] Alarm Set Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_SET_CONF", CCategory.ALARM_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D0", "Alarm Set Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_SET_REQ", CCategory.ALARM_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D0", "Alarm Set Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_SET_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D0", "Alarm Set ID");
            #endregion

            #region 7.1.2.9 [C2-9] Alarm Reset Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_RESET_CONF", CCategory.ALARM_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D1", "Alarm Reset Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_RESET_REQ", CCategory.ALARM_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D1", "Alarm Reset Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_RESET_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D1", "Alarm Reset ID");
            #endregion

            #region 7.1.2.10 [C2-10] Smoke Alarm Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_EQP_SMOKE_STATUS_CONF", CCategory.SMOKE_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=300F", "Eqp Smoke Detect Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_SMOKE_DETECT_REQ", CCategory.SMOKE_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=400F", "Eqp Smoke Dectect Request");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SMOKE_STATUS", CCategory.SMOKE_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=400F", "Eqp Smoke Status");
            #endregion

            #region 7.2.1.1 [G1-1]  Material Monitoring Data Report
            //__INTERNAL_VARIABLE_STRING("I_W_STAT_CHG_EVENT_CODE", $"{CCategory.MTRL_MONITER_DATA}", enumAccessType.In, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=55F0", "Material State Change Event Code");
            #endregion

            #region 7.2.3.5 [G3-5] Acutal Processing Data Report
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.APD_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3110", "Meas Cell Bcr Read Req Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.APD_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3110", "Meas Cell Bcr Read Req Confirm Ack");

            __INTERNAL_VARIABLE_SHORT("O_W_HOST_GBT_CELL", CCategory.APD_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3590,GROUP=APDCELL1", "Host GBT CELL");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_CELL_JUDG", CCategory.APD_RPT, enumAccessType.Out, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3591,GROUP=APDCELL1", "Cell Judge");
            __INTERNAL_VARIABLE_SHORT("O_W_IV_CA_JUDG", CCategory.APD_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3592,GROUP=APDCELL1", "IV CA Judge");
            __INTERNAL_VARIABLE_STRING("O_W_HOST_LVOLT_GRADE_INFO", CCategory.APD_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3593,LENGTH=2,GROUP=APDCELL1", "Low Volt Grade Info");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_LVOLT_FAULTY_INFO", CCategory.APD_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3594,GROUP=APDCELL1", "LVolt Faulty Info");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_CELL_JUDG_REWORK", CCategory.APD_RPT, enumAccessType.Out, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3595,GROUP=APDCELL1", "Cell Judge Rework");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_MV_DAY_JUDG", CCategory.APD_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3596,GROUP=APDCELL1", "MV Day Judg");
            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_MV_DAY_DATA", CCategory.APD_RPT, enumAccessType.Out, Int32.MaxValue, Int32.MinValue, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3597,GROUP=APDCELL1", "MV Day Data");
            __INTERNAL_VARIABLE_STRING("O_W_HOST_2D_BCR_DATA", CCategory.APD_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3599,LENGTH=100,GROUP=APDCELL1", "Cell#1 2D BCR Data");
            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_2D_BCR_LENGTH", CCategory.APD_RPT, enumAccessType.Out, Int32.MaxValue, Int32.MinValue, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=35CB,GROUP=APDCELL1", "2D BCR Length");
            __INTERNAL_VARIABLE_STRING("O_W_HOST_GBT_BCR_DATA", CCategory.APD_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35F0,LENGTH=100,GROUP=APDCELL1", "Cell#1 GBT BCR Data");
            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_GBT_BCR_LENGTH", CCategory.APD_RPT, enumAccessType.Out, Int32.MaxValue, Int32.MinValue, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3622,GROUP=APDCELL1", "GBT BCR Length");
            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_PRINT_MODE", CCategory.APD_RPT, enumAccessType.Out, Int32.MaxValue, Int32.MinValue, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3623,LENGTH=1,GROUP=APDCELL1", "Print Mode");
            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_MV_DAY_SPEC_DATA", CCategory.APD_RPT, enumAccessType.Out, Int32.MaxValue, Int32.MinValue, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3625,GROUP=APDCELL1", "MV Day Spec Data"); //2021.09.21 New

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.APD_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4110", "Ocv Meas Cell BCR Read Req");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", CCategory.APD_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4100", "Meas Cell Exist");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", CCategory.APD_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4680,LENGTH=10", "Meas Cell ID");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_LOT_ID", CCategory.APD_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46B0,LENGTH=16", "Meas Lot ID");
            __INTERNAL_VARIABLE_SHORT("I_W_THIC_POS", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5000", "Thick Position No");
            __INTERNAL_VARIABLE_INTEGER("I_W_THIC_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5001", "Thick Value");
            __INTERNAL_VARIABLE_INTEGER("I_W_VOLT_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5003", "Volt Value");
            __INTERNAL_VARIABLE_INTEGER("I_W_ACIR_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5005", "ACIR Value");
            __INTERNAL_VARIABLE_INTEGER("I_W_PRESS_OCV_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5007", "Press OCV Value ");
            __INTERNAL_VARIABLE_INTEGER("I_W_WEIGHT_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5009", "Weight Value");
            __INTERNAL_VARIABLE_INTEGER("I_W_IV_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=500B", "IV Value");
            __INTERNAL_VARIABLE_INTEGER("I_W_IR_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5019", "IR Value");
            __INTERNAL_VARIABLE_INTEGER("I_W_COLDPRESS_IR_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=501B", "Cold Press IR Value 1"); //2021.08.20 New
            __INTERNAL_VARIABLE_INTEGER("I_W_THIC1_SPEC_VAL1", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=500D", "Thick1 Value 1");
            __INTERNAL_VARIABLE_INTEGER("I_W_THIC1_SPEC_VAL2", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=500F", "Thick1 Value 2");
            __INTERNAL_VARIABLE_INTEGER("I_W_THIC1_SPEC_VAL3", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5011", "Thick1 Value 3");
            __INTERNAL_VARIABLE_INTEGER("I_W_THIC1_SPEC_VAL4", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5013", "Thick1 Value 4");
            __INTERNAL_VARIABLE_INTEGER("I_W_THIC_MAX_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5015", "Thick Max Value");
            __INTERNAL_VARIABLE_INTEGER("I_W_THIC_MIN_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5017", "Thick Min Value");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.APD_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4158", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4158", "Reading Type");

            #endregion

            #region 7.2.4.3 [G4-3] Cell Output Report
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CELL_OUT_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3045", "Bad Cell SSF OutPut Req");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CELL_OUT_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3045", "Cell SSF Output Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CELL_OUT_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4045", "Bad Cell SSF OutPut Req");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", CCategory.CELL_OUT_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4055", "Bad Cell SSF OutPut Exists");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", CCategory.CELL_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4640,LENGTH=20", "Bad Cell SSF Cell ID");
            __INTERNAL_VARIABLE_INTEGER("I_W_CELL_OUTPUT_INFO", CCategory.CELL_OUT_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4678,LENGTH=1", "Bad Cell SSF OutPut Info");
            __INTERNAL_VARIABLE_INTEGER("I_W_CELL_OUTPUT_JUDG", CCategory.CELL_OUT_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4670,LENGTH=1", "Bad Cell SSF Judge");
            #endregion

            #region 7.2.4.6 [G4-6] Cell Information Requeset
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CELL_INFO_REQ, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=304A", "Cell Info Req Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=304A", "Cell Info Req Ack");

            //$ 2025.08.25 : Melsec 연속 데이터를 Group으로 묶어 Bulk Insert 처리를 위해 속성 추가 [GROUP=INCELL1]
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID", CCategory.CELL_INFO_REQ, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3550,LENGTH=16,GROUP=INCELL1", "Input Lot ID");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID", CCategory.CELL_INFO_REQ, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3558,LENGTH=16,GROUP=INCELL1", "Input Group Lot ID");
            __INTERNAL_VARIABLE_STRING("O_W_PRODUCT_ID", CCategory.CELL_INFO_REQ, enumAccessType.Out, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3560,LENGTH=16,GROUP=INCELL1", "Product ID ");
            __INTERNAL_VARIABLE_STRING("O_W_MODEL_ID", CCategory.CELL_INFO_REQ, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3568,LENGTH=4,GROUP=INCELL1", "MODEL ID");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_CHECKITEM", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=356A,LENGTH=2,GROUP=INCELL1", "Check Item");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_TAB_CUT_USE1", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=356B,LENGTH=2,GROUP=INCELL1", "Tab Cutting Use1");
            __INTERNAL_VARIABLE_INTEGER("O_W_WEIGHT_SPEC_MAX", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=356C,GROUP=INCELL1", "Weight Spec Max");
            __INTERNAL_VARIABLE_INTEGER("O_W_WEIGHT_SPEC_MIN", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=356E,GROUP=INCELL1", "Weight Spec Min");
            __INTERNAL_VARIABLE_INTEGER("O_W_THIC_SPEC_MAX", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3570,GROUP=INCELL1", "Thick Spec Max");
            __INTERNAL_VARIABLE_INTEGER("O_W_THIC_SPEC_MIN", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3572,GROUP=INCELL1", "Thick Spec Min");
            __INTERNAL_VARIABLE_INTEGER("O_W_VOLT_SPEC_MAX", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3574,GROUP=INCELL1", "Volt Spec Max");
            __INTERNAL_VARIABLE_INTEGER("O_W_VOLT_SPEC_MIN", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3576,GROUP=INCELL1", "Volt Spec Min");
            __INTERNAL_VARIABLE_INTEGER("O_W_ACIR_SPEC_MAX", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3578,GROUP=INCELL1", "ACIR Spec Max");
            __INTERNAL_VARIABLE_INTEGER("O_W_ACIR_SPEC_MIN", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=357A,GROUP=INCELL1", "ACIR Spec Min");
            __INTERNAL_VARIABLE_INTEGER("O_W_IV_SPEC_MAX", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=357C,GROUP=INCELL1", "IV Spec Max");
            __INTERNAL_VARIABLE_INTEGER("O_W_IV_SPEC_MIN", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=357E,GROUP=INCELL1", "IV Spec Min");
            __INTERNAL_VARIABLE_INTEGER("O_W_PRESS_OCV_SPEC_MAX", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3580,GROUP=INCELL1", "Press OCV Spec Max");
            __INTERNAL_VARIABLE_INTEGER("O_W_PRESS_OCV_SPEC_MIN", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3582,GROUP=INCELL1", "Press OCV Spec Min");
            __INTERNAL_VARIABLE_INTEGER("O_W_IR_SPEC_MAX", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3584,GROUP=INCELL1", "IR Spec Max");
            __INTERNAL_VARIABLE_INTEGER("O_W_IR_SPEC_MIN", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3586,GROUP=INCELL1", "IR Spec Min");
            __INTERNAL_VARIABLE_INTEGER("O_W_COLDPRESS_IR_SPEC_MAX", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3588,GROUP=INCELL1", "Cold Press IR Spec Max"); //2021.08.20 New
            __INTERNAL_VARIABLE_INTEGER("O_W_COLDPRESS_IR_SPEC_MIN", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=358A,GROUP=INCELL1", "Cold Press IR Spec Min"); //2021.08.20 New

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CELL_INFO_REQ, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=404A", "Cell Info Req");

            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", CCategory.CELL_INFO_REQ, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4550,LENGTH=20", "Input Cell_ID1");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", CCategory.CELL_INFO_REQ, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=405A", "Input Cell Exists");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.CELL_INFO_REQ, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=415B", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.CELL_INFO_REQ, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=415B", "Reading Type");
            #endregion

            #region 7.2.4.6 [G4-6] Cell Information Requeset_03
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CELL_INFO_REQ_03, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=304C", "2D Cell Info Req Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CELL_INFO_REQ_03, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=304C", "2DCell Info Req Ack");

            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID", CCategory.CELL_INFO_REQ_03, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35D0,LENGTH=16,GROUP=2DCELL1", "Input Lot ID");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID", CCategory.CELL_INFO_REQ_03, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35D8,LENGTH=16,GROUP=2DCELL1", "Input Group Lot ID");
            __INTERNAL_VARIABLE_STRING("O_W_PRODUCT_ID", CCategory.CELL_INFO_REQ_03, enumAccessType.Out, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35E0,LENGTH=16,GROUP=2DCELL1", "Product ID");
            __INTERNAL_VARIABLE_STRING("O_W_MODEL_ID", CCategory.CELL_INFO_REQ_03, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35E8,LENGTH=4,GROUP=2DCELL1", "MODEL ID");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_CELL_2D_JUDG", CCategory.CELL_INFO_REQ_03, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=35EA,GROUP=2DCELL1", "2D_CELL_JUDGE");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_CELL_GBT_JUDG", CCategory.CELL_INFO_REQ_03, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=35EB,GROUP=2DCELL1", "GBT_CELL_JUDGE");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CELL_INFO_REQ_03, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=404C", "2D Cell Info Req");

            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", CCategory.CELL_INFO_REQ_03, enumAccessType.In, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=45AB,LENGTH=20", "2D GBT Cell_ID, ");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", CCategory.CELL_INFO_REQ_03, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=405C", "2D Cell Exists");
            __INTERNAL_VARIABLE_STRING("I_W_2D_VERIFY_GD", CCategory.CELL_INFO_REQ_03, enumAccessType.In, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=45B5,LENGTH=2", "2D Verify GRADE");
            __INTERNAL_VARIABLE_STRING("I_W_2D_VERIFY_DATA", CCategory.CELL_INFO_REQ_03, enumAccessType.In, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=45B6,LENGTH=100", "2D Verify Data");
            __INTERNAL_VARIABLE_INTEGER("I_W_2D_VERIFY_LENGTH", CCategory.CELL_INFO_REQ_03, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=45E8,LENGTH=1", "GBT Verify Length");
            __INTERNAL_VARIABLE_STRING("I_W_GBT_VERIFY_GD", CCategory.CELL_INFO_REQ_03, enumAccessType.In, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=45F0,LENGTH=2", "GBT Verify GRADE");
            __INTERNAL_VARIABLE_STRING("I_W_GBT_VERIFY_DATA", CCategory.CELL_INFO_REQ_03, enumAccessType.In, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=45F1,LENGTH=100", "GBT Verify Data");
            __INTERNAL_VARIABLE_INTEGER("I_W_GBT_VERIFY_LENGTH", CCategory.CELL_INFO_REQ_03, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4623,LENGTH=1", "GBT Verify Length");
            __INTERNAL_VARIABLE_INTEGER("I_W_PRINT_MODE1", CCategory.CELL_INFO_REQ_03, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4624,LENGTH=1", "Print Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.CELL_INFO_REQ_03, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=415C", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.CELL_INFO_REQ_03, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=415C", "Reading Type");
            #endregion
        }
        #endregion

        protected override void OnMessageReceived(IMessage request, string topic, string message)
        {
            EIF_Biz.OnMessageReceived(request, topic, message);
        }
    }

    public interface IEIF_Biz
    {
        void OnMessageReceived(IMessage request, string topic, string message);
    }

}