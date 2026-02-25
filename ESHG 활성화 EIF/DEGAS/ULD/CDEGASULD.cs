using LGCNS.ezControl.Common;
using LGCNS.ezControl.EIF.Solace;

using SolaceSystems.Solclient.Messaging;

using ESHG.EIF.FORM.COMMON;

namespace ESHG.EIF.FORM.DEGASULD
{
    public partial class CDEGASULD : CSolaceEIFServerBizRule
    {
        public IEIF_Biz EIF_Biz => (IEIF_Biz)Implement;

        #region Class Member variable
        public const string EQPTYPE = "DegasUnloader";  //$ 2021.07.05 : Modeler Element의 Nick과 반드시 일치 시키시오!!
        #endregion

        #region FactovaLync Method Override
        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            #region Virtual Variable
            __INTERNAL_VARIABLE_STRING("V_W_EQP_ID", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "EQP ID Main");
            __INTERNAL_VARIABLE_STRING("V_W_SUBEQP_ID_01", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "Sub EQP ID(Sealer)");
            __INTERNAL_VARIABLE_STRING("V_W_SUBEQP_ID_02", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "Sub EQP ID(HotPress)");
            __INTERNAL_VARIABLE_STRING("V_W_EXTEQP_ID", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "External EQP ID(Degas)"); //Degas는 Unloader Unit은 아니지만 Unloader에서 보고 필요한 외부 Unit

            __INTERNAL_VARIABLE_SHORT("V_TRAYINCELLCNT", "EQP_INFO", enumAccessType.Virtual, 0, 0, false, true, 0, "", "Tray Max Cell Count ex) 36, 30, 72"); //$ 2022.04.28 : ESWA4 대응을 위해 추가
            __INTERNAL_VARIABLE_INTEGERLIST("V_W_SYSTEM_SYNC_TIME", "SYSTEM", enumAccessType.Virtual, 4, 0, 0, false, false, 0, string.Empty, "SYSTEM Sync Time");

            __INTERNAL_VARIABLE_INTEGER("V_TACTTIME_INTERVAL", "EQP_INFO", enumAccessType.Virtual, 0, 0, false, true, 60, "", "TactTime Interval(Sec) - 0: Not Use"); //$ 2022.08.01 : Tacttime 수집 주기


            __INTERNAL_VARIABLE_BOOLEAN("V_IS_NAK_TEST", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 무조건 Nak Reply, False - Nak Test 사용 안함");           //$ 2023.01.05 : Test Mode 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_TIMEOUT_TEST", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 무조건 Time Out, False - Timeout Test 사용 안함");    //$ 2023.01.05 : Test Mode 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_ALMLOG_USE", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - Alarm Set/Rest EIF, Biz Log 남김, False - Log 사용 안함");    // HDH 2023.05.23 : Alarm Set/Rest Log 사용 유무

            __INTERNAL_VARIABLE_INTEGER("V_W_USE_TIME_SYNC", "SYSTEM", enumAccessType.Virtual, 24, 0, true, false, 0, string.Empty, "Data SYNC Time");  //2023.11.16 LWY : ESGM2 개발자 설정 시간동기화 추가 Virtual Var로 관리, 시간만 입력 가능 ( 1 ~ 23 )

            __INTERNAL_VARIABLE_SHORT("V_REMOTE_CMD", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.Virtual, 0, 0, true, true, 0, "", "Remote Command Send(1:RMS, 12: IT Bypass, 21: Pause");
            __INTERNAL_VARIABLE_SHORT("V_REMOTE_CMD", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.Virtual, 0, 0, true, true, 0, "", "Sealer Remote Command Send(1:RMS, 12: IT Bypass, 21: Pause");
            __INTERNAL_VARIABLE_SHORT("V_REMOTE_CMD", $"{CCategory.REMOTE_COMM_SND}_03", enumAccessType.Virtual, 0, 0, true, true, 0, "", "HotPress Remote Command Send(1:RMS, 12: IT Bypass, 21: Pause");

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

            #region 7.1.2.1 [C2-1] Equipment State Change Report_01 (Unloader)
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4010", "DGS ULD Equipment State");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SUBSTAT", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4012", "DGS ULD Equipment SubState");

            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4011", "DGS ULD Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_POPUP_DELAY_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4013", "DGS ULD User Stop Pop-up Delay Time");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_TACT_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4020", "DGS ULD EQP Tact Time");

            __INTERNAL_VARIABLE_STRING("I_W_CURRENT_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "DGS LD Current Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "DGS LD Current Group Lot ID");
            #endregion                              

            #region 7.1.2.1 [C2-1] Equipment State Change Report_02 (Sealer)
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4014", "Sealer Equipment State");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SUBSTAT", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4016", "Sealer Equipment SubState");

            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4015", "Sealer Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_POPUP_DELAY_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4017", "Sealer User Stop Pop-up Delay Time");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_TACT_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4030", "Sealer EQP Tact Time");

            //__INTERNAL_VARIABLE_STRING("I_W_CURRENT_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, false, true, string.Empty, "", "Sealer Current Lot ID");
            //__INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, false, true, string.Empty, "", "Sealer Current Group Lot ID");
            #endregion

            #region 7.1.2.1 [C2-1] Equipment State Change Report_03 (Hotpress)
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", $"{CCategory.EQP_STAT_CHG_RPT}_03", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4018", "Hotpress Equipment State");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SUBSTAT", $"{CCategory.EQP_STAT_CHG_RPT}_03", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=401A", "Hotpress Equipment SubState");

            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", $"{CCategory.EQP_STAT_CHG_RPT}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4019", "Hotpress Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_POPUP_DELAY_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=401B", "Hotpress User Stop Pop-up Delay Time");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_TACT_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4040", "Hotpress EQP Tact Time");

            //__INTERNAL_VARIABLE_STRING("I_W_CURRENT_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_03", enumAccessType.In, false, true, string.Empty, "", "Sealer Current Lot ID");
            //__INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_03", enumAccessType.In, false, true, string.Empty, "", "Sealer Current Group Lot ID");
            #endregion

            #region 7.1.2.2 [C2-2] Alarm Report_01
            //__INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Alarm ID");
            //__INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Equipment State");
            //__INTERNAL_VARIABLE_BOOLEAN("I_B_EQP_ALARM_TYPE", CCategory.ALARM_RP, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4008", "EQP Alarm Type");
            #endregion   

            #region 7.1.2.3 [C2-3] Host Alarm Message Send_01 (Unloader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_ALARM_MSG_SEND", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3002", "DGS ULD Host Alarm Msg Send");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_SEND_SYSTEM", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C00", "DGS ULD Alarm Send System");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_PROC_STOP_TYPE", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C01", "DGS ULD EQP Processing Stop Type");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_DISP_TYPE", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C02", "DGS ULD Host Alarm Display Type");

            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_ALARM_ID", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C03", "DGS ULD Host Alarm Code");
            __INTERNAL_VARIABLE_STRINGLIST("O_W_HOST_ALARM_MSG", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, 2, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=5C05,LENGTH=500, ENCODING=utf-16", "DGS ULD Host Alarm Message");
            #endregion

            #region 7.1.2.3 [C2-3] Host Alarm Message Send_02 (Sealer)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_ALARM_MSG_SEND", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3003", "Sealer Host Alarm Msg Send");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_SEND_SYSTEM", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W,ADDRESS_NO=5D00", "Sealer Alarm Send System");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_PROC_STOP_TYPE", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W,ADDRESS_NO=5D01", "Sealer EQP Processing Stop Type");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_DISP_TYPE", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W,ADDRESS_NO=5D02", "Sealer Host Alarm Display Type");

            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_ALARM_ID", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5D03", "Sealer Host Alarm Code");
            __INTERNAL_VARIABLE_STRINGLIST("O_W_HOST_ALARM_MSG", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, 2, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5D05,LENGTH=500, ENCODING=utf-16", "Sealer Host Alarm Message");
            #endregion

            #region 7.1.2.3 [C2-3] Host Alarm Message Send_03 (Hotpress)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_ALARM_MSG_SEND", "HOST_ALARM_MSG_SEND_03", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3004", "Hotpress Host Alarm Msg Send");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_SEND_SYSTEM", "HOST_ALARM_MSG_SEND_03", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W,ADDRESS_NO=5E00", "Hotpress Alarm Send System");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_PROC_STOP_TYPE", "HOST_ALARM_MSG_SEND_03", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W,ADDRESS_NO=5E01", "Hotpress EQP Processing Stop Type");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_DISP_TYPE", "HOST_ALARM_MSG_SEND_03", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W,ADDRESS_NO=5E02", "Hotpress Host Alarm Display Type");

            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_ALARM_ID", "HOST_ALARM_MSG_SEND_03", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5E03", "Hotpress Host Alarm Code");
            __INTERNAL_VARIABLE_STRINGLIST("O_W_HOST_ALARM_MSG", "HOST_ALARM_MSG_SEND_03", enumAccessType.Out, 2, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5E05,LENGTH=500, ENCODING=utf-16", "Hotpress Host Alarm Message");
            #endregion

            #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report_01 (Unloader)
            __INTERNAL_VARIABLE_BOOLEAN("I_B_AUTO_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4003", "DGS ULD Auto Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_IT_BYPASS", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4004", "DGS ULD IT Bypass");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REWORK_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4009", "DGS ULD Rework Mode");
            __INTERNAL_VARIABLE_SHORT("I_W_HMI_LANG_TYPE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4001", "DGS ULD HMI Language Type");
            #endregion                        

            #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report_02 (Sealer)
            __INTERNAL_VARIABLE_BOOLEAN("I_B_AUTO_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40A3", "Sealer Auto Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_IT_BYPASS", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40A4", "Sealer IT Bypass");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REWORK_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40A9", "Sealer Rework Mode");
            __INTERNAL_VARIABLE_SHORT("I_W_HMI_LANG_TYPE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4002", "Sealer HMI Language Type");
            #endregion

            #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report_03 (Hotpress)
            __INTERNAL_VARIABLE_BOOLEAN("I_B_AUTO_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40B3", "Hotpress Auto Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_IT_BYPASS", $"{CCategory.EQP_OP_MODE_CHG_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40B4", "Hotpress IT Bypass");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REWORK_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40B9", "Hotpress Rework Mode");
            __INTERNAL_VARIABLE_SHORT("I_W_HMI_LANG_TYPE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_03", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4003", "Hotpress HMI Language Type");
            #endregion

            #region 7.1.2.6 [C2-6] Remote Command Send_01 (Unloader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_REMOTE_COMMAND_SEND", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=300E", "DGS ULD Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("O_W_REMOTE_COMMAND_CODE", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=300E", "DGS ULD Remote Command Code");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REMOTE_COMMAND_CONF", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=400E", "DGS ULD Remote Command Confirm");
            __INTERNAL_VARIABLE_SHORT("I_W_REMOTE_COMMAND_CONF_ACK", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=400E", "DGS ULD Remote Command Confirm ACK");
            #endregion

            #region 7.1.2.6 [C2-6] Remote Command Send_02 (Sealer)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_REMOTE_COMMAND_SEND", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=309E", "Sealer Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("O_W_REMOTE_COMMAND_CODE", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=309E", "Sealer Remote Command Code");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REMOTE_COMMAND_CONF", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=409E", "Sealer Remote Command Confirm");
            __INTERNAL_VARIABLE_SHORT("I_W_REMOTE_COMMAND_CONF_ACK", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=409E", "Sealer Remote Command Confirm ACK");
            #endregion

            #region 7.1.2.6 [C2-6] Remote Command Send_03 (HotPress)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_REMOTE_COMMAND_SEND", $"{CCategory.REMOTE_COMM_SND}_03", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30BE", "HotPress Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("O_W_REMOTE_COMMAND_CODE", $"{CCategory.REMOTE_COMM_SND}_03", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=30BE", "HotPress Remote Command Code");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REMOTE_COMMAND_CONF", $"{CCategory.REMOTE_COMM_SND}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40BE", "HotPress Remote Command Confirm");
            __INTERNAL_VARIABLE_SHORT("I_W_REMOTE_COMMAND_CONF_ACK", $"{CCategory.REMOTE_COMM_SND}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40BE", "HotPress Remote Command Confirm ACK");
            #endregion

            #region 7.1.2.8 [C2-8] Alarm Set Report_01 (Unloader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_SET_CONF", $"{CCategory.ALARM_RPT}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D0", "DGS ULD Alarm Set Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_SET_REQ", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D0", "DGS ULD Alarm Set Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_SET_ID", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D0", "DGS ULD Alarm Set ID");
            #endregion

            #region 7.1.2.8 [C2-8] Alarm Set Report_02 (Sealer)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_SET_CONF", $"{CCategory.ALARM_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=30D2", "Sealer Alarm Set Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_SET_REQ", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40D2", "Sealer Alarm Set Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_SET_ID", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=40D2", "Sealer Alarm Set ID");
            #endregion

            #region 7.1.2.8 [C2-8] Alarm Set Report_03 (Hotpress)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_SET_CONF", $"{CCategory.ALARM_RPT}_03", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=30D4", "Hotpress Alarm Set Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_SET_REQ", $"{CCategory.ALARM_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40D4", "Hotpress Alarm Set Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_SET_ID", $"{CCategory.ALARM_RPT}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=40D4", "Hotpress Alarm Set ID");
            #endregion

            #region 7.1.2.9 [C2-9] Alarm Reset Report_01 (Unloader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_RESET_CONF", $"{CCategory.ALARM_RPT}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D1", "DGS ULD Alarm Reset Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_RESET_REQ", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D1", "DGS ULD Alarm Reset Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_RESET_ID", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D1", "DGS ULD Alarm Reset ID");
            #endregion

            #region 7.1.2.9 [C2-9] Alarm Reset Report_02 (Sealer)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_RESET_CONF", $"{CCategory.ALARM_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=30D3", "Sealer Alarm Reset Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_RESET_REQ", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40D3", "Sealer Alarm Reset Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_RESET_ID", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=40D3", "Sealer Alarm Reset ID");
            #endregion

            #region 7.1.2.9 [C2-9] Alarm Reset Report_03 (Hotpress)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_RESET_CONF", $"{CCategory.ALARM_RPT}_03", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=30D5", "Hotpress Alarm Reset Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_RESET_REQ", $"{CCategory.ALARM_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40D5", "Hotpress Alarm Reset Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_RESET_ID", $"{CCategory.ALARM_RPT}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=40D5", "Hotpress Alarm Reset ID");
            #endregion

            #region 7.1.2.10 [C2-10] Smoke Alarm Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_EQP_SMOKE_STATUS_CONF", CCategory.SMOKE_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=300F", "Eqp Smoke Detect Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_SMOKE_DETECT_REQ", CCategory.SMOKE_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=400F", "Eqp Smoke Dectect Request");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SMOKE_STATUS", CCategory.SMOKE_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=400F", "Eqp Smoke Status");
            #endregion

            #region 7.2.1.1 [G1-1]  Material Monitoring Data Report
            //__INTERNAL_VARIABLE_STRING("I_W_STAT_CHG_EVENT_CODE", CCategory.MTRL_MONITER_DATA, enumAccessType.In, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=55F0", "Material State Change Event Code");
            #endregion

            #region 7.2.2.2 [G2-2] Carrier Input Report
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CARR_IN_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3022", "Tray Input Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CARR_IN_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3022", "Tray Input Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CARR_IN_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4022", "Tray Input");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", CCategory.CARR_IN_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4160,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", CCategory.CARR_IN_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4020", "Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", CCategory.CARR_IN_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", CCategory.CARR_IN_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Group Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_PRODUCT_ID", CCategory.CARR_IN_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4100,LENGTH=16", "PRODUCTID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.CARR_IN_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4150", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.CARR_IN_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4150", "Reading Type");
            #endregion

            #region 7.2.2.3 [G2-3] Carrier Output Report
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CARR_OUT_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3024", "EQP Job Complete Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CARR_OUT_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3024", "EQP Job Complete Confirm");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CARR_OUT_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4024", "EQP Job Complete");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4180,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", CCategory.CARR_OUT_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4023", "Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Group Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_PRODUCT_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4100,LENGTH=16", "PRODUCTID");
            __INTERNAL_VARIABLE_STRINGLIST("I_W_TRAY_IN_CELLID_LIST", CCategory.CARR_OUT_RPT, enumAccessType.In, 48, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=41E0,LENGTH=20", "Tray In Cell ID List");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.CARR_OUT_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4150", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.CARR_OUT_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4150", "Reading Type");
            #endregion

            #region 7.2.3.5 [G3-5] Acutal Processing Data Report_01 (Degas + Sealer)
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.APD_RPT}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3110", "Degas + Sealer Meas Cell Bcr Read Req Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.APD_RPT}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3110", "Degas + Sealer Meas Cell Bcr Read Req Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.APD_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4110", "Degas + Sealer Meas Cell BCR Read Req");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST_01", $"{CCategory.APD_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4100", "Degas + Sealer Meas Cell Exist 01");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_01", $"{CCategory.APD_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4680,LENGTH=20", "Degas + Sealer Meas Cell ID 01");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_LOT_ID_01", $"{CCategory.APD_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46B0,LENGTH=16", "Degas + Sealer Meas Lot ID 01");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST_02", $"{CCategory.APD_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4101", "Degas + Sealer Meas Cell Exist 02");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_02", $"{CCategory.APD_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=468A,LENGTH=20", "Degas + Sealer Meas Cell ID 02");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_LOT_ID_02", $"{CCategory.APD_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46B8,LENGTH=16", "Degas + Sealer Meas Lot ID 02");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_REWORK", $"{CCategory.APD_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40A9", "Degas + Sealer Cell Rework");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.APD_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4158", "Degas + Sealer BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.APD_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4158", "Degas + Sealer Reading Type");

            #region APD DATA #01
            __INTERNAL_VARIABLE_SHORT("I_W_CHAMBER_NO1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5000", "Chamber No 1");
            __INTERNAL_VARIABLE_SHORT("I_W_CHAMBER_POS1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5001", "Chamber Pos 1");
            __INTERNAL_VARIABLE_SHORT("I_W_NEST_NO1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5002", "NEST_NO 1");
            __INTERNAL_VARIABLE_SHORT("I_W_DEGAS_PRESS_VAL1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5003", "DEGAS_PRESS_VAL 1");
            __INTERNAL_VARIABLE_SHORT("I_W_DEGAS_PRESS_TIME1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5004", "DEGAS_PRESS_TIME 1");
            __INTERNAL_VARIABLE_SHORT("I_W_VACUUM_REACH_TIME1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5005", "Vaccum REACH_TIME 1");
            __INTERNAL_VARIABLE_SHORT("I_W_VACUUM_KEEP_TIME1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5006", "Vaccum KEEP_TIME 1");
            __INTERNAL_VARIABLE_SIGNEDSHORT("I_W_VACUUM_DEGREE1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5007", "Vaccum DEGREE 1");
            __INTERNAL_VARIABLE_SHORT("I_W_VACUUM_VENT_TIME1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5008", "Vaccum VENT_TIME 1");
            __INTERNAL_VARIABLE_SHORT("I_W_CHAMBER_CYCLE_TIME1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5009", "Vaccum CHAMBER_CYCLE_TIME 1");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_UPPER_TEMP1_1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=500A", "PRE_SEALING_UPPER_TEMP 1_1");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_UPPER_TEMP2_1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=500B", "PRE_SEALING_UPPER_TEMP 2_1");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_UPPER_TEMP3_1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=500C", "PRE_SEALING_UPPER_TEMP 3_1");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_LOWER_TEMP1_1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=500D", "PRE_SEALING_LOWER_TEMP 1_1");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_LOWER_TEMP2_1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=500E", "PRE_SEALING_LOWER_TEMP 2_1");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_LOWER_TEMP3_1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=500F", "PRE_SEALING_LOWER_TEMP 3_1");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_TIME1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5010", "PRE_SEALING_TIME 1");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_PRESSURE1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5011", "PRE_SEALING_PRESSURE 1");
            __INTERNAL_VARIABLE_SHORT("I_W_MINUS_VENT_TIME1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5012", "MINUS VENT_TIME 1");
            __INTERNAL_VARIABLE_SHORT("I_W_1ST_VENT_TIME1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5013", "1st VENT_TIME 1");
            __INTERNAL_VARIABLE_SHORT("I_W_VENT_HOLD_TIME1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5014", "HOLD VENT_TIME 1");
            __INTERNAL_VARIABLE_SHORT("I_W_2ND_VENT_TIME1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5015", "2nd VENT_TIME 1");
            __INTERNAL_VARIABLE_SIGNEDSHORT("I_W_APRS_VALUE1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5016", "I_W_APRS_VALUE1");
            __INTERNAL_VARIABLE_SIGNEDSHORT("I_W_ABS_PRESS_VALUE1", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5017", "ABS_PRESS_VALUE1");
            #endregion

            #region APD DATA #02
            __INTERNAL_VARIABLE_SHORT("I_W_CHAMBER_NO2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5020", "Chamber No 2");
            __INTERNAL_VARIABLE_SHORT("I_W_CHAMBER_POS2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5021", "Chamber Pos 2");
            __INTERNAL_VARIABLE_SHORT("I_W_NEST_NO2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5022", "NEST_NO 2");
            __INTERNAL_VARIABLE_SHORT("I_W_DEGAS_PRESS_VAL2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5023", "DEGAS_PRESS_VAL 2");
            __INTERNAL_VARIABLE_SHORT("I_W_DEGAS_PRESS_TIME2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5024", "DEGAS_PRESS_TIME 2");
            __INTERNAL_VARIABLE_SHORT("I_W_VACUUM_REACH_TIME2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5025", "Vaccum REACH_TIME 2");
            __INTERNAL_VARIABLE_SHORT("I_W_VACUUM_KEEP_TIME2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5026", "Vaccum KEEP_TIME 2");
            __INTERNAL_VARIABLE_SIGNEDSHORT("I_W_VACUUM_DEGREE2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5027", "Vaccum DEGREE 2");
            __INTERNAL_VARIABLE_SHORT("I_W_VACUUM_VENT_TIME2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5028", "Vaccum VENT_TIME 2");
            __INTERNAL_VARIABLE_SHORT("I_W_CHAMBER_CYCLE_TIME2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5029", "Vaccum CHAMBER_CYCLE_TIME 2");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_UPPER_TEMP1_2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=502A", "PRE_SEALING_UPPER_TEMP 1_2");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_UPPER_TEMP2_2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=502B", "PRE_SEALING_UPPER_TEMP 2_2");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_UPPER_TEMP3_2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=502C", "PRE_SEALING_UPPER_TEMP 3_2");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_LOWER_TEMP1_2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=502D", "PRE_SEALING_LOWER_TEMP 1_2");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_LOWER_TEMP2_2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=502E", "PRE_SEALING_LOWER_TEMP 2_2");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_LOWER_TEMP3_2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=502F", "PRE_SEALING_LOWER_TEMP 3_2");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_TIME2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5030", "PRE_SEALING_TIME 2");
            __INTERNAL_VARIABLE_SHORT("I_W_PRE_SEALING_PRESSURE2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5031", "PRE_SEALING_PRESSURE 2");
            __INTERNAL_VARIABLE_SHORT("I_W_MINUS_VENT_TIME2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5032", "MINUS VENT_TIME 2");
            __INTERNAL_VARIABLE_SHORT("I_W_1ST_VENT_TIME2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5033", "1st VENT_TIME 2");
            __INTERNAL_VARIABLE_SHORT("I_W_VENT_HOLD_TIME2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5034", "HOLD VENT_TIME 2");
            __INTERNAL_VARIABLE_SHORT("I_W_2ND_VENT_TIME2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5035", "2nd VENT_TIME 2");
            __INTERNAL_VARIABLE_SIGNEDSHORT("I_W_APRS_VALUE2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5036", "I_W_APRS_VALUE2");
            __INTERNAL_VARIABLE_SIGNEDSHORT("I_W_ABS_PRESS_VALUE2", $"{CCategory.APD_RPT}_01_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5037", "ABS_PRESS_VALUE2");
            #endregion
            #endregion

            #region 7.2.3.5 [G3-5] Acutal Processing Data Report_02 (IV)
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.APD_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3111", "IV Meas Cell Bcr Read Req Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.APD_RPT}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3111", "IV Meas Cell Bcr Read Req Confirm Ack");
            __INTERNAL_VARIABLE_SHORT("O_W_CELL_JUDG_RESULT1", $"{CCategory.APD_RPT}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3390", "HotPress IV Cell#1 Faulty Judg");
            __INTERNAL_VARIABLE_SHORT("O_W_CELL_JUDG_RESULT2", $"{CCategory.APD_RPT}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3391", "HotPress IV Cell#2 Faulty Judg");
            __INTERNAL_VARIABLE_STRING("O_W_CELL_GRADE1", $"{CCategory.APD_RPT}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3392, LENGTH=1", "HotPress IV Cell#1 Grade");
            __INTERNAL_VARIABLE_STRING("O_W_CELL_GRADE2", $"{CCategory.APD_RPT}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3393, LENGTH=1", "HotPress IV Cell#2 Grade");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.APD_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4111", "IV Meas Cell BCR Read Req");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST_01", $"{CCategory.APD_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4102", "IV Meas Cell Exist 01");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_01", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4694,LENGTH=20", "IV Meas Cell ID 01");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_LOT_ID_01", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46C0,LENGTH=16", "IV Meas Lot ID 01");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST_02", $"{CCategory.APD_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4103", "IV Meas Cell Exist 02");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_02", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=469E,LENGTH=20", "IV Meas Cell ID 02");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_LOT_ID_02", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46C8,LENGTH=16", "IV Meas Lot ID 02");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_REWORK", $"{CCategory.APD_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40A9", "IV Cell Rework");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.APD_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4159", "IV BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.APD_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4159", "IV Reading Type");

            #region APD DATA #01
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_NO1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5040", "MAIN_SEALING_NO 1");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_UPPER_TEMP1_1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5041", "MAIN_SEALING_UPPER_TEMP1 1");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_UPPER_TEMP2_1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5042", "MAIN_SEALING_UPPER_TEMP2 1");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_UPPER_TEMP3_1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5043", "MAIN_SEALING_UPPER_TEMP3 1");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_LOWER_TEMP1_1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5044", "MAIN_SEALING_LOWER_TEMP1 1");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_LOWER_TEMP2_1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5045", "MAIN_SEALING_LOWER_TEMP2 1");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_LOWER_TEMP3_1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5046", "MAIN_SEALING_LOWER_TEMP3 1");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_TIME1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5047", "MAIN_SEALING_TIME 1");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_PRESSURE1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5048,LENGTH=1", "MAIN_SEALING_PRESSURE 1");
            __INTERNAL_VARIABLE_INTEGER("I_W_MAIN_SEALING_LOAD_CELL_PRESSURE1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5049", "MAIN_SEALING_LOAD_CELL_PRESSURE 1");

            __INTERNAL_VARIABLE_INTEGER("I_W_IV_WEIGHT_DATA1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5060", "IV Cell #1 Weight Data");
            __INTERNAL_VARIABLE_SHORT("I_W_IV_WEIGHT_POS1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5062", "IV Cell #1 Weight Position");
            __INTERNAL_VARIABLE_INTEGER("I_W_IV_DATA1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5063", "IV Cell #1 IV Data");
            __INTERNAL_VARIABLE_SHORT("I_W_IV_POS1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5065", "IV Cell #1 IV Position");
            __INTERNAL_VARIABLE_SHORT("I_W_IV_JUDG1", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5066", "IV Cell #1 IV Judg Result");
            #endregion

            #region APD DATA #02
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_NO2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5050", "MAIN_SEALING_NO 2");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_UPPER_TEMP1_2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5051", "MAIN_SEALING_UPPER_TEMP1 2");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_UPPER_TEMP2_2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5052", "MAIN_SEALING_UPPER_TEMP2 2");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_UPPER_TEMP3_2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5053", "MAIN_SEALING_UPPER_TEMP3 2");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_LOWER_TEMP1_2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5054", "MAIN_SEALING_LOWER_TEMP1 2");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_LOWER_TEMP2_2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5055", "MAIN_SEALING_LOWER_TEMP2 2");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_LOWER_TEMP3_2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5056", "MAIN_SEALING_LOWER_TEMP3 2");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_TIME2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5057", "MAIN_SEALING_TIME 2");
            __INTERNAL_VARIABLE_SHORT("I_W_MAIN_SEALING_PRESSURE2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5058", "MAIN_SEALING_PRESSURE 2");
            __INTERNAL_VARIABLE_INTEGER("I_W_MAIN_SEALING_LOAD_CELL_PRESSURE2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5059", "MAIN_SEALING_LOAD_CELL_PRESSURE 2");

            __INTERNAL_VARIABLE_INTEGER("I_W_IV_WEIGHT_DATA2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5070", "IV Cell #2 Weight Data");
            __INTERNAL_VARIABLE_SHORT("I_W_IV_WEIGHT_POS2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5072", "IV Cell #2 Weight Position");
            __INTERNAL_VARIABLE_INTEGER("I_W_IV_DATA2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5073", "IV Cell #2 IV Data");
            __INTERNAL_VARIABLE_SHORT("I_W_IV_POS2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5075", "IV Cell #2 IV Position");
            __INTERNAL_VARIABLE_SHORT("I_W_IV_JUDG2", $"{CCategory.APD_RPT}_02_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5076", "IV Cell #2 IV Judg Result");
            #endregion
            #endregion

            #region 7.2.3.5 [G3-5] Acutal Processing Data Report_03 (Hotpress)
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.APD_RPT}_03", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3112", "Hotpress Meas Cell Bcr Read Req Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.APD_RPT}_03", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3112", "Hotpress Meas Cell Bcr Read Req Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.APD_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4112", "Hotpress Meas Cell BCR Read Req");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST_01", $"{CCategory.APD_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4104", "Hotpress Meas Cell Exist 01");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_01", $"{CCategory.APD_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46F0,LENGTH=20", "Hotpress Meas Cell ID 01");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_LOT_ID_01", $"{CCategory.APD_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46D0,LENGTH=16", "Hotpress Meas Lot ID 01");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST_02", $"{CCategory.APD_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4105", "Hotpress Meas Cell Exist 02");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_02", $"{CCategory.APD_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46FA,LENGTH=20", "Hotpress Meas Cell ID 02");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_LOT_ID_02", $"{CCategory.APD_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46D8,LENGTH=16", "Hotpress Meas Lot ID 02");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_REWORK", $"{CCategory.APD_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40B9", "Hotpress Cell Rework");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.APD_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=415A", "Hotpress BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.APD_RPT}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=415A", "Hotpress Reading Type");

            #region APD DATA #01
            __INTERNAL_VARIABLE_SHORT("I_W_PORT_NO1", $"{CCategory.APD_RPT}_03_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5080", "HotPress Cell #1 Port No");
            __INTERNAL_VARIABLE_SHORT("I_W_PRESS_UPPER_TEMP1", $"{CCategory.APD_RPT}_03_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5081", "HotPress Cell #1 Press Upper TEMP");
            __INTERNAL_VARIABLE_SHORT("I_W_PRESS_VAL1", $"{CCategory.APD_RPT}_03_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5082", "HotPress Cell #1 Press VAL");
            __INTERNAL_VARIABLE_SHORT("I_W_PRESS_TIME1", $"{CCategory.APD_RPT}_03_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5083", "HotPress Cell #1 Press TIME");
            __INTERNAL_VARIABLE_SHORT("I_W_PRESS_LOWER_TEMP1", $"{CCategory.APD_RPT}_03_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5084", "HotPress Cell #1 Press Lower TEMP");
            #endregion

            #region APD DATA #02
            __INTERNAL_VARIABLE_SHORT("I_W_PORT_NO2", $"{CCategory.APD_RPT}_03_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5090", "HotPress Cell #2 Port No");
            __INTERNAL_VARIABLE_SHORT("I_W_PRESS_UPPER_TEMP2", $"{CCategory.APD_RPT}_03_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5091", "HotPress Cell #2 Press Upper TEMP");
            __INTERNAL_VARIABLE_SHORT("I_W_PRESS_VAL2", $"{CCategory.APD_RPT}_03_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5092", "HotPress Cell #2 Press VAL");
            __INTERNAL_VARIABLE_SHORT("I_W_PRESS_TIME2", $"{CCategory.APD_RPT}_03_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5093", "HotPress Cell #2 Press TIME");
            __INTERNAL_VARIABLE_SHORT("I_W_PRESS_LOWER_TEMP2", $"{CCategory.APD_RPT}_03_DATA", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5094", "HotPress Cell #2 Press Lower TEMP");
            #endregion
            #endregion

            #region 7.2.4.3 [G4-3] Cell Output Report_01 (외관 검사 모드 - Sealer 후 배출)
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.Out, false, true, true, "DEVICE_TYPE=B,ADDRESS_NO=3045", "Sealer Bad Cell Output Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3045", "Sealer Bad  Cell Output Conf Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4045", "Sealer Bad  Cell Output");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4055", "Sealer Bad  Cell Exist");
            __INTERNAL_VARIABLE_SHORT("I_W_CELL_OUT_RESULT_01", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4678,LENGTH=1", "Sealer Bad  Cell Out Result Info 01");
            __INTERNAL_VARIABLE_SHORT("I_W_CELL_OUT_RESULT_02", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=467A,LENGTH=1", "Sealer Bad Cell Out Result Info 02");
            __INTERNAL_VARIABLE_SHORT("I_W_CELL_OUT_DEFECTCD_01", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4670,LENGTH=1", "Sealer Bad Cell Out Defect Code 01");
            __INTERNAL_VARIABLE_SHORT("I_W_CELL_OUT_DEFECTCD_02", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4672,LENGTH=1", "Sealer Bad Cell Out Defect Code 02");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_01", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4640,LENGTH=20", $"Sealer Bad Data Report Cell ID 01");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_02", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=464A,LENGTH=20", $"Sealer Bad Data Report Cell ID 02");
            #endregion

            #region 7.2.4.3 [G4-3] Cell Output Report_02 (NG 배출 - IV 후 배출)
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.Out, false, true, true, "DEVICE_TYPE=B,ADDRESS_NO=3046", "IV Bad Cell Output Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3046", "IV Bad Cell Output Conf Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4046", "IV Bad Cell Output");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4056", "IV Bad Cell Exist");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3108,LENGTH=16", "IV Bad Group Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=30F8,LENGTH=16", "IV Bad Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_PRODUCT_ID", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3100,LENGTH=16", "IV Bad Product ID");
            __INTERNAL_VARIABLE_SHORT("I_W_CELL_OUT_RESULT_01", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4679,LENGTH=1", "IV Bad Cell Out Result Info 01");
            __INTERNAL_VARIABLE_SHORT("I_W_CELL_OUT_RESULT_02", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=467B,LENGTH=1", "IV Bad Cell Out Result Info 02");
            __INTERNAL_VARIABLE_SHORT("I_W_CELL_OUT_DEFECTCD_01", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4671,LENGTH=1", "IV Bad Cell Out Defect Code 01");
            __INTERNAL_VARIABLE_SHORT("I_W_CELL_OUT_DEFECTCD_02", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4673,LENGTH=1", "IV Bad Cell Out Defect Code 02");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_01", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4654,LENGTH=20", $"IV Bad Data Report Cell ID 01");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_02", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=465E,LENGTH=20", $"IV Bad Data Report Cell ID 02");
            #endregion

            #region 7.2.4.6 [G4-6] Cell Information Requeset
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CELL_INFO_REQ, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=304A", "Cell Info Req Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=304A", "Cell Info Req Ack");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", CCategory.CELL_INFO_REQ, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3558,LENGTH=16", "Group Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_PRODUCT_ID", CCategory.CELL_INFO_REQ, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3560,LENGTH=16", "Product ID");
            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CELL_INFO_REQ, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=404A", "Cell Info Req");

            __INTERNAL_VARIABLE_STRING("I_W_FIRST_CELL_ID", CCategory.CELL_INFO_REQ, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4550,LENGTH=20", "First Cell ID");
            __INTERNAL_VARIABLE_STRINGLIST("I_W_CELLID_LIST", CCategory.CELL_INFO_REQ, enumAccessType.In, 2, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=455A,LENGTH=20", "CELL_ID_LIST");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.CELL_INFO_REQ, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=415D", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.CELL_INFO_REQ, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=415D", "Reading Type");
            #endregion

            #region ETC 
            //JH 2025.08.04 디가스 역전 현상 방지를 위한 DEGLD IO 변수 생성 
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"DEG_LD_{CCategory.CARR_OUT_RPT}", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3024", "DEG LD EQP Job Complete Confirm");
            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"DEG_LD_{CCategory.CARR_OUT_RPT}", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4024", "DEG LD Job Complete");

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