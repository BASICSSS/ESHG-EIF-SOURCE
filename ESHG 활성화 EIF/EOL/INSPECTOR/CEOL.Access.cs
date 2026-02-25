using System.Collections.Generic;
using System.ComponentModel;

using LGCNS.ezControl.Core;
using ESHG.EIF.FORM.EOL;
using ESHG.EIF.FORM.COMMON;

namespace ESHG.EIF.FORM.EOL
{


    /// <summary>
    /// CEOL의 요약
    /// </summary>
    public partial class CEOL
    {

        /// <summary>
        /// Component의 모든 구성요소가 Instancing완료 되었을 때 호출
        /// </summary>
        protected override void OnInstancing()
        {
            base.OnInstancing();
        }

        #region Properties for Variable Access
        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SERVER_CONNECTION
        {
            get
            {
                return this["SERVER_CONNECTION"];
            }
        }

        /// <summary>
        /// Host System Interface Adapter
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_ADAPTER_TYPE
        {
            get
            {
                return this["HOST_SERVICE:HOST_ADAPTER_TYPE"];
            }
        }

        /// <summary>
        /// Host Connection Infomation
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_CONNECTION_INFO
        {
            get
            {
                return this["HOST_SERVICE:HOST_CONNECTION_INFO"];
            }
        }

        /// <summary>
        /// Host Message
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_MESSAGE
        {
            get
            {
                return this["HOST_SERVICE:HOST_MESSAGE"];
            }
        }

        /// <summary>
        /// Host Error Code
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_ERROR_CODE
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_CODE"];
            }
        }

        /// <summary>
        /// Host Error Data
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_ERROR_DATA
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_DATA"];
            }
        }

        /// <summary>
        /// Host Error Parameter
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_ERROR_PARA
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_PARA"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__CONNECTION_INFO
        {
            get
            {
                return this["SOLACE:CONNECTION_INFO"];
            }
        }

        /// <summary>
        /// Message Log 활성화 여부
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__ENABLE_LOG
        {
            get
            {
                return this["SOLACE:ENABLE_LOG"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__HOST_ERROR_CODE
        {
            get
            {
                return this["SOLACE:HOST_ERROR_CODE"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__HOST_ERROR_MESSAGE
        {
            get
            {
                return this["SOLACE:HOST_ERROR_MESSAGE"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__HOST_ERROR_BIZ
        {
            get
            {
                return this["SOLACE:HOST_ERROR_BIZ"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__HOST_ERROR_DATA
        {
            get
            {
                return this["SOLACE:HOST_ERROR_DATA"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__HOST_ERROR_LOC
        {
            get
            {
                return this["SOLACE:HOST_ERROR_LOC"];
            }
        }

        /// <summary>
        /// EQP ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_INFO__V_W_EQP_ID
        {
            get
            {
                return this["EQP_INFO:V_W_EQP_ID"];
            }
        }

        /// <summary>
        /// TactTime Interval(Sec) - 0: Not Use
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_INFO__V_TACTTIME_INTERVAL
        {
            get
            {
                return this["EQP_INFO:V_TACTTIME_INTERVAL"];
            }
        }

        /// <summary>
        /// SYSTEM Sync Time
        /// </summary>
        [Browsable(false)]
        public CVariable @__SYSTEM__V_W_SYSTEM_SYNC_TIME
        {
            get
            {
                return this["SYSTEM:V_W_SYSTEM_SYNC_TIME"];
            }
        }

        /// <summary>
        /// Data SYNC Time
        /// </summary>
        [Browsable(false)]
        public CVariable @__SYSTEM__V_W_USE_TIME_SYNC
        {
            get
            {
                return this["SYSTEM:V_W_USE_TIME_SYNC"];
            }
        }

        /// <summary>
        /// Remote Command Send
        /// </summary>
        [Browsable(false)]
        public CVariable @__REMOTE_COMM_SND__V_REMOTE_CMD
        {
            get
            {
                return this["REMOTE_COMM_SND:V_REMOTE_CMD"];
            }
        }

        /// <summary>
        /// 2D PRINT USE Y/N MODE
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_INFO__V_W_PRINTER_USE
        {
            get
            {
                return this["EQP_INFO:V_W_PRINTER_USE"];
            }
        }

        /// <summary>
        /// True - 무조건 Nak Reply, False - Nak Test 사용 안함
        /// </summary>
        [Browsable(false)]
        public CVariable @__TESTMODE__V_IS_NAK_TEST
        {
            get
            {
                return this["TESTMODE:V_IS_NAK_TEST"];
            }
        }

        /// <summary>
        /// True - 무조건 Time Out, False - Timeout Test 사용 안함
        /// </summary>
        [Browsable(false)]
        public CVariable @__TESTMODE__V_IS_TIMEOUT_TEST
        {
            get
            {
                return this["TESTMODE:V_IS_TIMEOUT_TEST"];
            }
        }

        /// <summary>
        /// True - Alarm Set/Rest EIF, Biz Log 남김, False - Log 사용 안함
        /// </summary>
        [Browsable(false)]
        public CVariable @__TESTMODE__V_IS_ALMLOG_USE
        {
            get
            {
                return this["TESTMODE:V_IS_ALMLOG_USE"];
            }
        }

        /// <summary>
        /// EQP Comm Check
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_COMM_CHK__I_W_EQP_COMM_CHK
        {
            get
            {
                return this["EQP_COMM_CHK:I_W_EQP_COMM_CHK"];
            }
        }

        /// <summary>
        /// Host Comm Check
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_COMM_CHK__O_B_HOST_COMM_CHK
        {
            get
            {
                return this["HOST_COMM_CHK:O_B_HOST_COMM_CHK"];
            }
        }

        /// <summary>
        /// Host Comm Check Confirm
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF
        {
            get
            {
                return this["HOST_COMM_CHK:I_B_HOST_COMM_CHK_CONF"];
            }
        }

        /// <summary>
        /// COMM ON
        /// </summary>
        [Browsable(false)]
        public CVariable @__COMM_STAT_CHG_RPT__I_B_COMM_ON
        {
            get
            {
                return this["COMM_STAT_CHG_RPT:I_B_COMM_ON"];
            }
        }

        /// <summary>
        /// COMM OFF
        /// </summary>
        [Browsable(false)]
        public CVariable @__COMM_STAT_CHG_RPT__I_B_COMM_OFF
        {
            get
            {
                return this["COMM_STAT_CHG_RPT:I_B_COMM_OFF"];
            }
        }

        /// <summary>
        /// Date Time Set Request
        /// </summary>
        [Browsable(false)]
        public CVariable @__DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ
        {
            get
            {
                return this["DATE_TIME_SET_REQ:O_B_DATE_TIME_SET_REQ"];
            }
        }

        /// <summary>
        /// Date and Time
        /// </summary>
        [Browsable(false)]
        public CVariable @__DATE_TIME_SET_REQ__O_W_DATE_TIME
        {
            get
            {
                return this["DATE_TIME_SET_REQ:O_W_DATE_TIME"];
            }
        }

        /// <summary>
        /// Equipment State
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_STAT_CHG_RPT__I_W_EQP_STAT
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_EQP_STAT"];
            }
        }

        /// <summary>
        /// Equipment SubState
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_EQP_SUBSTAT"];
            }
        }

        /// <summary>
        /// Alarm ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_STAT_CHG_RPT__I_W_ALARM_ID
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_ALARM_ID"];
            }
        }

        /// <summary>
        /// User Stop Pop-up Delay Time
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_STAT_CHG_RPT__I_W_POPUP_DELAY_TIME
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_POPUP_DELAY_TIME"];
            }
        }

        /// <summary>
        /// EQP Tact Time
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_STAT_CHG_RPT__I_W_EQP_TACT_TIME
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_EQP_TACT_TIME"];
            }
        }

        /// <summary>
        /// Current Lot ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_STAT_CHG_RPT__I_W_CURRENT_LOT_ID
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_CURRENT_LOT_ID"];
            }
        }

        /// <summary>
        /// Current Group Lot ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_STAT_CHG_RPT__I_W_GROUP_LOT_ID
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_GROUP_LOT_ID"];
            }
        }

        /// <summary>
        /// Host Alarm Msg Send
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_ALARM_MSG_SEND__O_B_HOST_ALARM_MSG_SEND
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND"];
            }
        }

        /// <summary>
        /// Alarm Send System
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_SEND_SYSTEM
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_SEND_SYSTEM"];
            }
        }

        /// <summary>
        /// EQP Processing Stop Type
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_ALARM_MSG_SEND__O_W_EQP_PROC_STOP_TYPE
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_W_EQP_PROC_STOP_TYPE"];
            }
        }

        /// <summary>
        /// Host Alarm Display Type
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_DISP_TYPE
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_DISP_TYPE"];
            }
        }

        /// <summary>
        /// Host Alarm Code
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_ID
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_ID"];
            }
        }

        /// <summary>
        /// Host Alarm Message
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_MSG
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"];
            }
        }

        /// <summary>
        /// Auto Mode
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE
        {
            get
            {
                return this["EQP_OP_MODE_CHG_RPT:I_B_AUTO_MODE"];
            }
        }

        /// <summary>
        /// IT Bypass
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_OP_MODE_CHG_RPT__I_B_IT_BYPASS
        {
            get
            {
                return this["EQP_OP_MODE_CHG_RPT:I_B_IT_BYPASS"];
            }
        }

        /// <summary>
        /// Rework Mode
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_OP_MODE_CHG_RPT__I_B_REWORK_MODE
        {
            get
            {
                return this["EQP_OP_MODE_CHG_RPT:I_B_REWORK_MODE"];
            }
        }

        /// <summary>
        /// HMI Language Type
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_OP_MODE_CHG_RPT__I_W_HMI_LANG_TYPE
        {
            get
            {
                return this["EQP_OP_MODE_CHG_RPT:I_W_HMI_LANG_TYPE"];
            }
        }

        /// <summary>
        /// Remote Command Send
        /// </summary>
        [Browsable(false)]
        public CVariable @__REMOTE_COMM_SND__O_B_REMOTE_COMMAND_SEND
        {
            get
            {
                return this["REMOTE_COMM_SND:O_B_REMOTE_COMMAND_SEND"];
            }
        }

        /// <summary>
        /// Remote Command Code
        /// </summary>
        [Browsable(false)]
        public CVariable @__REMOTE_COMM_SND__O_W_REMOTE_COMMAND_CODE
        {
            get
            {
                return this["REMOTE_COMM_SND:O_W_REMOTE_COMMAND_CODE"];
            }
        }

        /// <summary>
        /// Remote Command Confirm
        /// </summary>
        [Browsable(false)]
        public CVariable @__REMOTE_COMM_SND__I_B_REMOTE_COMMAND_CONF
        {
            get
            {
                return this["REMOTE_COMM_SND:I_B_REMOTE_COMMAND_CONF"];
            }
        }

        /// <summary>
        /// Remote Command Confirm ACK
        /// </summary>
        [Browsable(false)]
        public CVariable @__REMOTE_COMM_SND__I_W_REMOTE_COMMAND_CONF_ACK
        {
            get
            {
                return this["REMOTE_COMM_SND:I_W_REMOTE_COMMAND_CONF_ACK"];
            }
        }

        /// <summary>
        /// Alarm Set Confirm
        /// </summary>
        [Browsable(false)]
        public CVariable @__ALARM_RPT__O_B_ALARM_SET_CONF
        {
            get
            {
                return this["ALARM_RPT:O_B_ALARM_SET_CONF"];
            }
        }

        /// <summary>
        /// Alarm Set Request
        /// </summary>
        [Browsable(false)]
        public CVariable @__ALARM_RPT__I_B_ALARM_SET_REQ
        {
            get
            {
                return this["ALARM_RPT:I_B_ALARM_SET_REQ"];
            }
        }

        /// <summary>
        /// Alarm Set ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__ALARM_RPT__I_W_ALARM_SET_ID
        {
            get
            {
                return this["ALARM_RPT:I_W_ALARM_SET_ID"];
            }
        }

        /// <summary>
        /// Alarm Reset Confirm
        /// </summary>
        [Browsable(false)]
        public CVariable @__ALARM_RPT__O_B_ALARM_RESET_CONF
        {
            get
            {
                return this["ALARM_RPT:O_B_ALARM_RESET_CONF"];
            }
        }

        /// <summary>
        /// Alarm Reset Request
        /// </summary>
        [Browsable(false)]
        public CVariable @__ALARM_RPT__I_B_ALARM_RESET_REQ
        {
            get
            {
                return this["ALARM_RPT:I_B_ALARM_RESET_REQ"];
            }
        }

        /// <summary>
        /// Alarm Reset ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__ALARM_RPT__I_W_ALARM_RESET_ID
        {
            get
            {
                return this["ALARM_RPT:I_W_ALARM_RESET_ID"];
            }
        }

        /// <summary>
        /// Eqp Smoke Detect Confirm
        /// </summary>
        [Browsable(false)]
        public CVariable @__SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF
        {
            get
            {
                return this["SMOKE_RPT:O_B_EQP_SMOKE_STATUS_CONF"];
            }
        }

        /// <summary>
        /// Eqp Smoke Dectect Request
        /// </summary>
        [Browsable(false)]
        public CVariable @__SMOKE_RPT__I_B_SMOKE_DETECT_REQ
        {
            get
            {
                return this["SMOKE_RPT:I_B_SMOKE_DETECT_REQ"];
            }
        }

        /// <summary>
        /// Eqp Smoke Status
        /// </summary>
        [Browsable(false)]
        public CVariable @__SMOKE_RPT__I_W_EQP_SMOKE_STATUS
        {
            get
            {
                return this["SMOKE_RPT:I_W_EQP_SMOKE_STATUS"];
            }
        }

        /// <summary>
        /// Meas Cell Bcr Read Req Confirm
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_B_TRIGGER_REPORT_CONF
        {
            get
            {
                return this["G3_5_APD_RPT:O_B_TRIGGER_REPORT_CONF"];
            }
        }

        /// <summary>
        /// Meas Cell Bcr Read Req Confirm Ack
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_TRIGGER_REPORT_ACK
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_TRIGGER_REPORT_ACK"];
            }
        }

        /// <summary>
        /// Host GBT CELL
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_GBT_CELL
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_GBT_CELL"];
            }
        }

        /// <summary>
        /// Cell Judge
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_EQP_CELL_JUDG
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_EQP_CELL_JUDG"];
            }
        }

        /// <summary>
        /// IV CA Judge
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_IV_CA_JUDG
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_IV_CA_JUDG"];
            }
        }

        /// <summary>
        /// Low Volt Grade Info
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_LVOLT_GRADE_INFO
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_LVOLT_GRADE_INFO"];
            }
        }

        /// <summary>
        /// LVolt Faulty Info
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_LVOLT_FAULTY_INFO
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_LVOLT_FAULTY_INFO"];
            }
        }

        /// <summary>
        /// Cell Judge Rework
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_EQP_CELL_JUDG_REWORK
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_EQP_CELL_JUDG_REWORK"];
            }
        }

        /// <summary>
        /// MV Day Judg
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_MV_DAY_JUDG
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_MV_DAY_JUDG"];
            }
        }

        /// <summary>
        /// MV Day Data
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_MV_DAY_DATA
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_MV_DAY_DATA"];
            }
        }

        /// <summary>
        /// Cell#1 2D BCR Data
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_2D_BCR_DATA
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_2D_BCR_DATA"];
            }
        }

        /// <summary>
        /// 2D BCR Length
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_2D_BCR_LENGTH
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_2D_BCR_LENGTH"];
            }
        }

        /// <summary>
        /// Cell#1 GBT BCR Data
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_GBT_BCR_DATA
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_GBT_BCR_DATA"];
            }
        }

        /// <summary>
        /// GBT BCR Length
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_GBT_BCR_LENGTH
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_GBT_BCR_LENGTH"];
            }
        }

        /// <summary>
        /// Print Mode
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_PRINT_MODE
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_PRINT_MODE"];
            }
        }

        /// <summary>
        /// MV Day Spec Data
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__O_W_HOST_MV_DAY_SPEC_DATA
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_MV_DAY_SPEC_DATA"];
            }
        }

        /// <summary>
        /// Ocv Meas Cell BCR Read Req
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_B_TRIGGER_REPORT
        {
            get
            {
                return this["G3_5_APD_RPT:I_B_TRIGGER_REPORT"];
            }
        }

        /// <summary>
        /// Meas Cell Exist
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_B_CELL_EXIST
        {
            get
            {
                return this["G3_5_APD_RPT:I_B_CELL_EXIST"];
            }
        }

        /// <summary>
        /// Meas Cell ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_CELL_ID
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_CELL_ID"];
            }
        }

        /// <summary>
        /// Meas Lot ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_CELL_LOT_ID
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_CELL_LOT_ID"];
            }
        }

        /// <summary>
        /// Thick Position No
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_THIC_POS
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC_POS"];
            }
        }

        /// <summary>
        /// Thick Value
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_THIC_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC_VAL"];
            }
        }

        /// <summary>
        /// Volt Value
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_VOLT_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_VOLT_VAL"];
            }
        }

        /// <summary>
        /// ACIR Value
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_ACIR_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_ACIR_VAL"];
            }
        }

        /// <summary>
        /// Press OCV Value 
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_PRESS_OCV_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_PRESS_OCV_VAL"];
            }
        }

        /// <summary>
        /// Weight Value
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_WEIGHT_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_WEIGHT_VAL"];
            }
        }

        /// <summary>
        /// IV Value
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_IV_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_IV_VAL"];
            }
        }

        /// <summary>
        /// IR Value
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_IR_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_IR_VAL"];
            }
        }

        /// <summary>
        /// Cold Press IR Value 1
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_COLDPRESS_IR_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_COLDPRESS_IR_VAL"];
            }
        }

        /// <summary>
        /// Thick1 Value 1
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_THIC1_SPEC_VAL1
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC1_SPEC_VAL1"];
            }
        }

        /// <summary>
        /// Thick1 Value 2
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_THIC1_SPEC_VAL2
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC1_SPEC_VAL2"];
            }
        }

        /// <summary>
        /// Thick1 Value 3
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_THIC1_SPEC_VAL3
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC1_SPEC_VAL3"];
            }
        }

        /// <summary>
        /// Thick1 Value 4
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_THIC1_SPEC_VAL4
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC1_SPEC_VAL4"];
            }
        }

        /// <summary>
        /// Thick Max Value
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_THIC_MAX_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC_MAX_VAL"];
            }
        }

        /// <summary>
        /// Thick Min Value
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_THIC_MIN_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC_MIN_VAL"];
            }
        }

        /// <summary>
        /// BCR Using
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_B_BCR_USING
        {
            get
            {
                return this["G3_5_APD_RPT:I_B_BCR_USING"];
            }
        }

        /// <summary>
        /// Reading Type
        /// </summary>
        [Browsable(false)]
        public CVariable @__G3_5_APD_RPT__I_W_READING_TYPE
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_READING_TYPE"];
            }
        }

        /// <summary>
        /// Bad Cell SSF OutPut Req
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_3_CELL_OUT_RPT__O_B_TRIGGER_REPORT_CONF
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:O_B_TRIGGER_REPORT_CONF"];
            }
        }

        /// <summary>
        /// Cell SSF Output Confirm Ack
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_3_CELL_OUT_RPT__O_W_TRIGGER_REPORT_ACK
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:O_W_TRIGGER_REPORT_ACK"];
            }
        }

        /// <summary>
        /// Bad Cell SSF OutPut Req
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_3_CELL_OUT_RPT__I_B_TRIGGER_REPORT
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:I_B_TRIGGER_REPORT"];
            }
        }

        /// <summary>
        /// Bad Cell SSF OutPut Exists
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_3_CELL_OUT_RPT__I_B_CELL_EXIST
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:I_B_CELL_EXIST"];
            }
        }

        /// <summary>
        /// Bad Cell SSF Cell ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_3_CELL_OUT_RPT__I_W_CELL_ID
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:I_W_CELL_ID"];
            }
        }

        /// <summary>
        /// Bad Cell SSF OutPut Info
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_3_CELL_OUT_RPT__I_W_CELL_OUTPUT_INFO
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:I_W_CELL_OUTPUT_INFO"];
            }
        }


        /// <summary>
        /// Bad Cell SSF Judge
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_3_CELL_OUT_RPT__I_W_CELL_OUTPUT_JUDG
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:I_W_CELL_OUTPUT_JUDG"];
            }
        }

        /// <summary>
        /// Cell Info Req Conf
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_B_TRIGGER_REPORT_CONF"];
            }
        }

        /// <summary>
        /// Cell Info Req Ack
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_TRIGGER_REPORT_ACK
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_TRIGGER_REPORT_ACK"];
            }
        }

        /// <summary>
        /// Input Lot ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_LOT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_LOT_ID"];
            }
        }

        /// <summary>
        /// Input Group Lot ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_GROUP_LOT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_GROUP_LOT_ID"];
            }
        }

        /// <summary>
        /// Product ID 
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_PRODUCT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_PRODUCT_ID"];
            }
        }

        /// <summary>
        /// MODEL ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_MODEL_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_MODEL_ID"];
            }
        }

        /// <summary>
        /// Check Item
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_HOST_CHECKITEM
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_HOST_CHECKITEM"];
            }
        }

        /// <summary>
        /// Tab Cutting Use1
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_HOST_TAB_CUT_USE1
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_HOST_TAB_CUT_USE1"];
            }
        }

        /// <summary>
        /// Weight Spec Max
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_WEIGHT_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_WEIGHT_SPEC_MAX"];
            }
        }

        /// <summary>
        /// Weight Spec Min
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_WEIGHT_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_WEIGHT_SPEC_MIN"];
            }
        }

        /// <summary>
        /// Thick Spec Max
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_THIC_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_THIC_SPEC_MAX"];
            }
        }

        /// <summary>
        /// Thick Spec Min
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_THIC_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_THIC_SPEC_MIN"];
            }
        }

        /// <summary>
        /// Volt Spec Max
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_VOLT_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_VOLT_SPEC_MAX"];
            }
        }

        /// <summary>
        /// Volt Spec Min
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_VOLT_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_VOLT_SPEC_MIN"];
            }
        }

        /// <summary>
        /// ACIR Spec Max
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_ACIR_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_ACIR_SPEC_MAX"];
            }
        }

        /// <summary>
        /// ACIR Spec Min
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_ACIR_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_ACIR_SPEC_MIN"];
            }
        }

        /// <summary>
        /// IV Spec Max
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_IV_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_IV_SPEC_MAX"];
            }
        }

        /// <summary>
        /// IV Spec Min
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_IV_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_IV_SPEC_MIN"];
            }
        }

        /// <summary>
        /// Press OCV Spec Max
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_PRESS_OCV_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_PRESS_OCV_SPEC_MAX"];
            }
        }

        /// <summary>
        /// Press OCV Spec Min
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_PRESS_OCV_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_PRESS_OCV_SPEC_MIN"];
            }
        }

        /// <summary>
        /// IR Spec Max
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_IR_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_IR_SPEC_MAX"];
            }
        }

        /// <summary>
        /// IR Spec Min
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_IR_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_IR_SPEC_MIN"];
            }
        }

        /// <summary>
        /// Cold Press IR Spec Max
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_COLDPRESS_IR_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_COLDPRESS_IR_SPEC_MAX"];
            }
        }

        /// <summary>
        /// Cold Press IR Spec Min
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__O_W_COLDPRESS_IR_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_COLDPRESS_IR_SPEC_MIN"];
            }
        }

        /// <summary>
        /// Cell Info Req
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__I_B_TRIGGER_REPORT
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:I_B_TRIGGER_REPORT"];
            }
        }

        /// <summary>
        /// Input Cell_ID1
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__I_W_CELL_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:I_W_CELL_ID"];
            }
        }

        /// <summary>
        /// Input Cell Exists
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__I_B_CELL_EXIST
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:I_B_CELL_EXIST"];
            }
        }

        /// <summary>
        /// BCR Using
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__I_B_BCR_USING
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:I_B_BCR_USING"];
            }
        }

        /// <summary>
        /// Reading Type
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ__I_W_READING_TYPE
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:I_W_READING_TYPE"];
            }
        }

        /// <summary>
        /// 2D Cell Info Req Conf
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__O_B_TRIGGER_REPORT_CONF
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_B_TRIGGER_REPORT_CONF"];
            }
        }

        /// <summary>
        /// 2DCell Info Req Ack
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__O_W_TRIGGER_REPORT_ACK
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_TRIGGER_REPORT_ACK"];
            }
        }

        /// <summary>
        /// Input Lot ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__O_W_LOT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_LOT_ID"];
            }
        }

        /// <summary>
        /// Input Group Lot ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__O_W_GROUP_LOT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_GROUP_LOT_ID"];
            }
        }

        /// <summary>
        /// Product ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__O_W_PRODUCT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_PRODUCT_ID"];
            }
        }

        /// <summary>
        /// MODEL ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__O_W_MODEL_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_MODEL_ID"];
            }
        }

        /// <summary>
        /// 2D_CELL_JUDGE
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__O_W_HOST_CELL_2D_JUDG
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_HOST_CELL_2D_JUDG"];
            }
        }

        /// <summary>
        /// GBT_CELL_JUDGE
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__O_W_HOST_CELL_GBT_JUDG
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_HOST_CELL_GBT_JUDG"];
            }
        }

        /// <summary>
        /// 2D Cell Info Req
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_B_TRIGGER_REPORT
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_B_TRIGGER_REPORT"];
            }
        }

        /// <summary>
        /// 2D GBT Cell_ID, 
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_W_CELL_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_CELL_ID"];
            }
        }

        /// <summary>
        /// 2D Cell Exists
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_B_CELL_EXIST
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_B_CELL_EXIST"];
            }
        }

        /// <summary>
        /// 2D Verify GRADE
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_W_2D_VERIFY_GD
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_2D_VERIFY_GD"];
            }
        }

        /// <summary>
        /// 2D Verify Data
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_W_2D_VERIFY_DATA
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_2D_VERIFY_DATA"];
            }
        }

        /// <summary>
        /// GBT Verify Length
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_W_2D_VERIFY_LENGTH
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_2D_VERIFY_LENGTH"];
            }
        }

        /// <summary>
        /// GBT Verify GRADE
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_W_GBT_VERIFY_GD
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_GBT_VERIFY_GD"];
            }
        }

        /// <summary>
        /// GBT Verify Data
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_W_GBT_VERIFY_DATA
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_GBT_VERIFY_DATA"];
            }
        }

        /// <summary>
        /// GBT Verify Length
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_W_GBT_VERIFY_LENGTH
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_GBT_VERIFY_LENGTH"];
            }
        }

        /// <summary>
        /// Print Mode
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_W_PRINT_MODE1
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_PRINT_MODE1"];
            }
        }

        /// <summary>
        /// BCR Using
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_B_BCR_USING
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_B_BCR_USING"];
            }
        }

        /// <summary>
        /// Reading Type
        /// </summary>
        [Browsable(false)]
        public CVariable @__G4_6_CELL_INFO_REQ_03__I_W_READING_TYPE
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_READING_TYPE"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable ___PLC03___IS_CONNECTED
        {
            get
            {
                return this["[PLC03]:IS_CONNECTED"];
            }
        }
        #endregion


        #region Properties for Variable Access By Value
        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : List<bool>)
        /// </summary>
        [Browsable(false)]
        public System.Collections.Generic.List<bool> SERVER_CONNECTION
        {
            get
            {
                return this["SERVER_CONNECTION"].AsBooleanList;
            }
            set
            {
                this["SERVER_CONNECTION"].AsBooleanList = value;
            }
        }

        /// <summary>
        /// Host System Interface Adapter
        ///  (Access Type : In/Out, Data Type : )
        /// </summary>
        [Browsable(false)]
        public HOST_ADAPTER_TYPE HOST_SERVICE__HOST_ADAPTER_TYPE
        {
            get
            {
                return (HOST_ADAPTER_TYPE)this["HOST_SERVICE:HOST_ADAPTER_TYPE"].AsEnum;
            }
            set
            {
                this["HOST_SERVICE:HOST_ADAPTER_TYPE"].AsEnum = (int)value;
            }
        }

        /// <summary>
        /// Host Connection Infomation
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string HOST_SERVICE__HOST_CONNECTION_INFO
        {
            get
            {
                return this["HOST_SERVICE:HOST_CONNECTION_INFO"].AsString;
            }
            set
            {
                this["HOST_SERVICE:HOST_CONNECTION_INFO"].AsString = value;
            }
        }

        /// <summary>
        /// Host Message
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string HOST_SERVICE__HOST_MESSAGE
        {
            get
            {
                return this["HOST_SERVICE:HOST_MESSAGE"].AsString;
            }
            set
            {
                this["HOST_SERVICE:HOST_MESSAGE"].AsString = value;
            }
        }

        /// <summary>
        /// Host Error Code
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string HOST_SERVICE__HOST_ERROR_CODE
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_CODE"].AsString;
            }
            set
            {
                this["HOST_SERVICE:HOST_ERROR_CODE"].AsString = value;
            }
        }

        /// <summary>
        /// Host Error Data
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string HOST_SERVICE__HOST_ERROR_DATA
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_DATA"].AsString;
            }
            set
            {
                this["HOST_SERVICE:HOST_ERROR_DATA"].AsString = value;
            }
        }

        /// <summary>
        /// Host Error Parameter
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string HOST_SERVICE__HOST_ERROR_PARA
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_PARA"].AsString;
            }
            set
            {
                this["HOST_SERVICE:HOST_ERROR_PARA"].AsString = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__CONNECTION_INFO
        {
            get
            {
                return this["SOLACE:CONNECTION_INFO"].AsString;
            }
            set
            {
                this["SOLACE:CONNECTION_INFO"].AsString = value;
            }
        }

        /// <summary>
        /// Message Log 활성화 여부
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool SOLACE__ENABLE_LOG
        {
            get
            {
                return this["SOLACE:ENABLE_LOG"].AsBoolean;
            }
            set
            {
                this["SOLACE:ENABLE_LOG"].AsBoolean = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__HOST_ERROR_CODE
        {
            get
            {
                return this["SOLACE:HOST_ERROR_CODE"].AsString;
            }
            set
            {
                this["SOLACE:HOST_ERROR_CODE"].AsString = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__HOST_ERROR_MESSAGE
        {
            get
            {
                return this["SOLACE:HOST_ERROR_MESSAGE"].AsString;
            }
            set
            {
                this["SOLACE:HOST_ERROR_MESSAGE"].AsString = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__HOST_ERROR_BIZ
        {
            get
            {
                return this["SOLACE:HOST_ERROR_BIZ"].AsString;
            }
            set
            {
                this["SOLACE:HOST_ERROR_BIZ"].AsString = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__HOST_ERROR_DATA
        {
            get
            {
                return this["SOLACE:HOST_ERROR_DATA"].AsString;
            }
            set
            {
                this["SOLACE:HOST_ERROR_DATA"].AsString = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__HOST_ERROR_LOC
        {
            get
            {
                return this["SOLACE:HOST_ERROR_LOC"].AsString;
            }
            set
            {
                this["SOLACE:HOST_ERROR_LOC"].AsString = value;
            }
        }

        /// <summary>
        /// EQP ID
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string EQP_INFO__V_W_EQP_ID
        {
            get
            {
                return this["EQP_INFO:V_W_EQP_ID"].AsString;
            }
            set
            {
                this["EQP_INFO:V_W_EQP_ID"].AsString = value;
            }
        }

        /// <summary>
        /// TactTime Interval(Sec) - 0: Not Use
        ///  (Access Type : In/Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int EQP_INFO__V_TACTTIME_INTERVAL
        {
            get
            {
                return this["EQP_INFO:V_TACTTIME_INTERVAL"].AsInteger;
            }
            set
            {
                this["EQP_INFO:V_TACTTIME_INTERVAL"].AsInteger = value;
            }
        }

        /// <summary>
        /// SYSTEM Sync Time
        ///  (Access Type : In/Out, Data Type : List<int>)
        /// </summary>
        [Browsable(false)]
        public System.Collections.Generic.List<int> SYSTEM__V_W_SYSTEM_SYNC_TIME
        {
            get
            {
                return this["SYSTEM:V_W_SYSTEM_SYNC_TIME"].AsIntegerList;
            }
            set
            {
                this["SYSTEM:V_W_SYSTEM_SYNC_TIME"].AsIntegerList = value;
            }
        }

        /// <summary>
        /// Data SYNC Time
        ///  (Access Type : In/Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int SYSTEM__V_W_USE_TIME_SYNC
        {
            get
            {
                return this["SYSTEM:V_W_USE_TIME_SYNC"].AsInteger;
            }
            set
            {
                this["SYSTEM:V_W_USE_TIME_SYNC"].AsInteger = value;
            }
        }

        /// <summary>
        /// Remote Command Send
        ///  (Access Type : In/Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort REMOTE_COMM_SND__V_REMOTE_CMD
        {
            get
            {
                return this["REMOTE_COMM_SND:V_REMOTE_CMD"].AsShort;
            }
            set
            {
                this["REMOTE_COMM_SND:V_REMOTE_CMD"].AsShort = value;
            }
        }

        /// <summary>
        /// 2D PRINT USE Y/N MODE
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool EQP_INFO__V_W_PRINTER_USE
        {
            get
            {
                return this["EQP_INFO:V_W_PRINTER_USE"].AsBoolean;
            }
            set
            {
                this["EQP_INFO:V_W_PRINTER_USE"].AsBoolean = value;
            }
        }

        /// <summary>
        /// True - 무조건 Nak Reply, False - Nak Test 사용 안함
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool TESTMODE__V_IS_NAK_TEST
        {
            get
            {
                return this["TESTMODE:V_IS_NAK_TEST"].AsBoolean;
            }
            set
            {
                this["TESTMODE:V_IS_NAK_TEST"].AsBoolean = value;
            }
        }

        /// <summary>
        /// True - 무조건 Time Out, False - Timeout Test 사용 안함
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool TESTMODE__V_IS_TIMEOUT_TEST
        {
            get
            {
                return this["TESTMODE:V_IS_TIMEOUT_TEST"].AsBoolean;
            }
            set
            {
                this["TESTMODE:V_IS_TIMEOUT_TEST"].AsBoolean = value;
            }
        }

        /// <summary>
        /// True - Alarm Set/Rest EIF, Biz Log 남김, False - Log 사용 안함
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool TESTMODE__V_IS_ALMLOG_USE
        {
            get
            {
                return this["TESTMODE:V_IS_ALMLOG_USE"].AsBoolean;
            }
            set
            {
                this["TESTMODE:V_IS_ALMLOG_USE"].AsBoolean = value;
            }
        }

        /// <summary>
        /// EQP Comm Check
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort EQP_COMM_CHK__I_W_EQP_COMM_CHK
        {
            get
            {
                return this["EQP_COMM_CHK:I_W_EQP_COMM_CHK"].AsShort;
            }
        }

        /// <summary>
        /// Host Comm Check
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool HOST_COMM_CHK__O_B_HOST_COMM_CHK
        {
            get
            {
                return this["HOST_COMM_CHK:O_B_HOST_COMM_CHK"].AsBoolean;
            }
            set
            {
                this["HOST_COMM_CHK:O_B_HOST_COMM_CHK"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Host Comm Check Confirm
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF
        {
            get
            {
                return this["HOST_COMM_CHK:I_B_HOST_COMM_CHK_CONF"].AsBoolean;
            }
        }

        /// <summary>
        /// COMM ON
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool COMM_STAT_CHG_RPT__I_B_COMM_ON
        {
            get
            {
                return this["COMM_STAT_CHG_RPT:I_B_COMM_ON"].AsBoolean;
            }
        }

        /// <summary>
        /// COMM OFF
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool COMM_STAT_CHG_RPT__I_B_COMM_OFF
        {
            get
            {
                return this["COMM_STAT_CHG_RPT:I_B_COMM_OFF"].AsBoolean;
            }
        }

        /// <summary>
        /// Date Time Set Request
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ
        {
            get
            {
                return this["DATE_TIME_SET_REQ:O_B_DATE_TIME_SET_REQ"].AsBoolean;
            }
            set
            {
                this["DATE_TIME_SET_REQ:O_B_DATE_TIME_SET_REQ"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Date and Time
        ///  (Access Type : Out, Data Type : List<ushort>)
        /// </summary>
        [Browsable(false)]
        public System.Collections.Generic.List<ushort> DATE_TIME_SET_REQ__O_W_DATE_TIME
        {
            get
            {
                return this["DATE_TIME_SET_REQ:O_W_DATE_TIME"].AsShortList;
            }
            set
            {
                this["DATE_TIME_SET_REQ:O_W_DATE_TIME"].AsShortList = value;
            }
        }

        /// <summary>
        /// Equipment State
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort EQP_STAT_CHG_RPT__I_W_EQP_STAT
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_EQP_STAT"].AsShort;
            }
        }

        /// <summary>
        /// Equipment SubState
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_EQP_SUBSTAT"].AsShort;
            }
        }

        /// <summary>
        /// Alarm ID
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort EQP_STAT_CHG_RPT__I_W_ALARM_ID
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_ALARM_ID"].AsShort;
            }
        }

        /// <summary>
        /// User Stop Pop-up Delay Time
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort EQP_STAT_CHG_RPT__I_W_POPUP_DELAY_TIME
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_POPUP_DELAY_TIME"].AsShort;
            }
        }

        /// <summary>
        /// EQP Tact Time
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort EQP_STAT_CHG_RPT__I_W_EQP_TACT_TIME
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_EQP_TACT_TIME"].AsShort;
            }
        }

        /// <summary>
        /// Current Lot ID
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string EQP_STAT_CHG_RPT__I_W_CURRENT_LOT_ID
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_CURRENT_LOT_ID"].AsString;
            }
        }

        /// <summary>
        /// Current Group Lot ID
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string EQP_STAT_CHG_RPT__I_W_GROUP_LOT_ID
        {
            get
            {
                return this["EQP_STAT_CHG_RPT:I_W_GROUP_LOT_ID"].AsString;
            }
        }

        /// <summary>
        /// Host Alarm Msg Send
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool HOST_ALARM_MSG_SEND__O_B_HOST_ALARM_MSG_SEND
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND"].AsBoolean;
            }
            set
            {
                this["HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Alarm Send System
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_SEND_SYSTEM
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_SEND_SYSTEM"].AsShort;
            }
            set
            {
                this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_SEND_SYSTEM"].AsShort = value;
            }
        }

        /// <summary>
        /// EQP Processing Stop Type
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort HOST_ALARM_MSG_SEND__O_W_EQP_PROC_STOP_TYPE
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_W_EQP_PROC_STOP_TYPE"].AsShort;
            }
            set
            {
                this["HOST_ALARM_MSG_SEND:O_W_EQP_PROC_STOP_TYPE"].AsShort = value;
            }
        }

        /// <summary>
        /// Host Alarm Display Type
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_DISP_TYPE
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_DISP_TYPE"].AsShort;
            }
            set
            {
                this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_DISP_TYPE"].AsShort = value;
            }
        }

        /// <summary>
        /// Host Alarm Code
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_ID
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_ID"].AsInteger;
            }
            set
            {
                this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_ID"].AsInteger = value;
            }
        }

        /// <summary>
        /// Host Alarm Message
        ///  (Access Type : Out, Data Type : List<string>)
        /// </summary>
        [Browsable(false)]
        public System.Collections.Generic.List<string> HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_MSG
        {
            get
            {
                return this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].AsStringList;
            }
            set
            {
                this["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].AsStringList = value;
            }
        }

        /// <summary>
        /// Auto Mode
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE
        {
            get
            {
                return this["EQP_OP_MODE_CHG_RPT:I_B_AUTO_MODE"].AsBoolean;
            }
        }

        /// <summary>
        /// IT Bypass
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool EQP_OP_MODE_CHG_RPT__I_B_IT_BYPASS
        {
            get
            {
                return this["EQP_OP_MODE_CHG_RPT:I_B_IT_BYPASS"].AsBoolean;
            }
        }

        /// <summary>
        /// Rework Mode
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool EQP_OP_MODE_CHG_RPT__I_B_REWORK_MODE
        {
            get
            {
                return this["EQP_OP_MODE_CHG_RPT:I_B_REWORK_MODE"].AsBoolean;
            }
        }

        /// <summary>
        /// HMI Language Type
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort EQP_OP_MODE_CHG_RPT__I_W_HMI_LANG_TYPE
        {
            get
            {
                return this["EQP_OP_MODE_CHG_RPT:I_W_HMI_LANG_TYPE"].AsShort;
            }
        }

        /// <summary>
        /// Remote Command Send
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool REMOTE_COMM_SND__O_B_REMOTE_COMMAND_SEND
        {
            get
            {
                return this["REMOTE_COMM_SND:O_B_REMOTE_COMMAND_SEND"].AsBoolean;
            }
            set
            {
                this["REMOTE_COMM_SND:O_B_REMOTE_COMMAND_SEND"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Remote Command Code
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort REMOTE_COMM_SND__O_W_REMOTE_COMMAND_CODE
        {
            get
            {
                return this["REMOTE_COMM_SND:O_W_REMOTE_COMMAND_CODE"].AsShort;
            }
            set
            {
                this["REMOTE_COMM_SND:O_W_REMOTE_COMMAND_CODE"].AsShort = value;
            }
        }

        /// <summary>
        /// Remote Command Confirm
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool REMOTE_COMM_SND__I_B_REMOTE_COMMAND_CONF
        {
            get
            {
                return this["REMOTE_COMM_SND:I_B_REMOTE_COMMAND_CONF"].AsBoolean;
            }
        }

        /// <summary>
        /// Remote Command Confirm ACK
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort REMOTE_COMM_SND__I_W_REMOTE_COMMAND_CONF_ACK
        {
            get
            {
                return this["REMOTE_COMM_SND:I_W_REMOTE_COMMAND_CONF_ACK"].AsShort;
            }
        }

        /// <summary>
        /// Alarm Set Confirm
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool ALARM_RPT__O_B_ALARM_SET_CONF
        {
            get
            {
                return this["ALARM_RPT:O_B_ALARM_SET_CONF"].AsBoolean;
            }
            set
            {
                this["ALARM_RPT:O_B_ALARM_SET_CONF"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Alarm Set Request
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool ALARM_RPT__I_B_ALARM_SET_REQ
        {
            get
            {
                return this["ALARM_RPT:I_B_ALARM_SET_REQ"].AsBoolean;
            }
        }

        /// <summary>
        /// Alarm Set ID
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort ALARM_RPT__I_W_ALARM_SET_ID
        {
            get
            {
                return this["ALARM_RPT:I_W_ALARM_SET_ID"].AsShort;
            }
        }

        /// <summary>
        /// Alarm Reset Confirm
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool ALARM_RPT__O_B_ALARM_RESET_CONF
        {
            get
            {
                return this["ALARM_RPT:O_B_ALARM_RESET_CONF"].AsBoolean;
            }
            set
            {
                this["ALARM_RPT:O_B_ALARM_RESET_CONF"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Alarm Reset Request
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool ALARM_RPT__I_B_ALARM_RESET_REQ
        {
            get
            {
                return this["ALARM_RPT:I_B_ALARM_RESET_REQ"].AsBoolean;
            }
        }

        /// <summary>
        /// Alarm Reset ID
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort ALARM_RPT__I_W_ALARM_RESET_ID
        {
            get
            {
                return this["ALARM_RPT:I_W_ALARM_RESET_ID"].AsShort;
            }
        }

        /// <summary>
        /// Eqp Smoke Detect Confirm
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF
        {
            get
            {
                return this["SMOKE_RPT:O_B_EQP_SMOKE_STATUS_CONF"].AsBoolean;
            }
            set
            {
                this["SMOKE_RPT:O_B_EQP_SMOKE_STATUS_CONF"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Eqp Smoke Dectect Request
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool SMOKE_RPT__I_B_SMOKE_DETECT_REQ
        {
            get
            {
                return this["SMOKE_RPT:I_B_SMOKE_DETECT_REQ"].AsBoolean;
            }
        }

        /// <summary>
        /// Eqp Smoke Status
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort SMOKE_RPT__I_W_EQP_SMOKE_STATUS
        {
            get
            {
                return this["SMOKE_RPT:I_W_EQP_SMOKE_STATUS"].AsShort;
            }
        }

        /// <summary>
        /// Meas Cell Bcr Read Req Confirm
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G3_5_APD_RPT__O_B_TRIGGER_REPORT_CONF
        {
            get
            {
                return this["G3_5_APD_RPT:O_B_TRIGGER_REPORT_CONF"].AsBoolean;
            }
            set
            {
                this["G3_5_APD_RPT:O_B_TRIGGER_REPORT_CONF"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Meas Cell Bcr Read Req Confirm Ack
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G3_5_APD_RPT__O_W_TRIGGER_REPORT_ACK
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_TRIGGER_REPORT_ACK"].AsShort;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_TRIGGER_REPORT_ACK"].AsShort = value;
            }
        }

        /// <summary>
        /// Host GBT CELL
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G3_5_APD_RPT__O_W_HOST_GBT_CELL
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_GBT_CELL"].AsShort;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_GBT_CELL"].AsShort = value;
            }
        }

        /// <summary>
        /// Cell Judge
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G3_5_APD_RPT__O_W_EQP_CELL_JUDG
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_EQP_CELL_JUDG"].AsShort;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_EQP_CELL_JUDG"].AsShort = value;
            }
        }

        /// <summary>
        /// IV CA Judge
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G3_5_APD_RPT__O_W_IV_CA_JUDG
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_IV_CA_JUDG"].AsShort;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_IV_CA_JUDG"].AsShort = value;
            }
        }

        /// <summary>
        /// Low Volt Grade Info
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G3_5_APD_RPT__O_W_HOST_LVOLT_GRADE_INFO
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_LVOLT_GRADE_INFO"].AsString;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_LVOLT_GRADE_INFO"].AsString = value;
            }
        }

        /// <summary>
        /// LVolt Faulty Info
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G3_5_APD_RPT__O_W_HOST_LVOLT_FAULTY_INFO
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_LVOLT_FAULTY_INFO"].AsShort;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_LVOLT_FAULTY_INFO"].AsShort = value;
            }
        }

        /// <summary>
        /// Cell Judge Rework
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G3_5_APD_RPT__O_W_EQP_CELL_JUDG_REWORK
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_EQP_CELL_JUDG_REWORK"].AsShort;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_EQP_CELL_JUDG_REWORK"].AsShort = value;
            }
        }

        /// <summary>
        /// MV Day Judg
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G3_5_APD_RPT__O_W_HOST_MV_DAY_JUDG
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_MV_DAY_JUDG"].AsShort;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_MV_DAY_JUDG"].AsShort = value;
            }
        }

        /// <summary>
        /// MV Day Data
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__O_W_HOST_MV_DAY_DATA
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_MV_DAY_DATA"].AsInteger;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_MV_DAY_DATA"].AsInteger = value;
            }
        }

        /// <summary>
        /// Cell#1 2D BCR Data
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G3_5_APD_RPT__O_W_HOST_2D_BCR_DATA
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_2D_BCR_DATA"].AsString;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_2D_BCR_DATA"].AsString = value;
            }
        }

        /// <summary>
        /// 2D BCR Length
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__O_W_HOST_2D_BCR_LENGTH
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_2D_BCR_LENGTH"].AsInteger;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_2D_BCR_LENGTH"].AsInteger = value;
            }
        }

        /// <summary>
        /// Cell#1 GBT BCR Data
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G3_5_APD_RPT__O_W_HOST_GBT_BCR_DATA
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_GBT_BCR_DATA"].AsString;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_GBT_BCR_DATA"].AsString = value;
            }
        }

        /// <summary>
        /// GBT BCR Length
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__O_W_HOST_GBT_BCR_LENGTH
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_GBT_BCR_LENGTH"].AsInteger;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_GBT_BCR_LENGTH"].AsInteger = value;
            }
        }

        /// <summary>
        /// Print Mode
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__O_W_HOST_PRINT_MODE
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_PRINT_MODE"].AsInteger;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_PRINT_MODE"].AsInteger = value;
            }
        }

        /// <summary>
        /// MV Day Spec Data
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__O_W_HOST_MV_DAY_SPEC_DATA
        {
            get
            {
                return this["G3_5_APD_RPT:O_W_HOST_MV_DAY_SPEC_DATA"].AsInteger;
            }
            set
            {
                this["G3_5_APD_RPT:O_W_HOST_MV_DAY_SPEC_DATA"].AsInteger = value;
            }
        }

        /// <summary>
        /// Ocv Meas Cell BCR Read Req
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G3_5_APD_RPT__I_B_TRIGGER_REPORT
        {
            get
            {
                return this["G3_5_APD_RPT:I_B_TRIGGER_REPORT"].AsBoolean;
            }
        }

        /// <summary>
        /// Meas Cell Exist
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G3_5_APD_RPT__I_B_CELL_EXIST
        {
            get
            {
                return this["G3_5_APD_RPT:I_B_CELL_EXIST"].AsBoolean;
            }
        }

        /// <summary>
        /// Meas Cell ID
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G3_5_APD_RPT__I_W_CELL_ID
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_CELL_ID"].AsString;
            }
        }

        /// <summary>
        /// Meas Lot ID
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G3_5_APD_RPT__I_W_CELL_LOT_ID
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_CELL_LOT_ID"].AsString;
            }
        }

        /// <summary>
        /// Thick Position No
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G3_5_APD_RPT__I_W_THIC_POS
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC_POS"].AsShort;
            }
        }

        /// <summary>
        /// Thick Value
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_THIC_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC_VAL"].AsInteger;
            }
        }

        /// <summary>
        /// Volt Value
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_VOLT_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_VOLT_VAL"].AsInteger;
            }
        }

        /// <summary>
        /// ACIR Value
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_ACIR_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_ACIR_VAL"].AsInteger;
            }
        }

        /// <summary>
        /// Press OCV Value 
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_PRESS_OCV_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_PRESS_OCV_VAL"].AsInteger;
            }
        }

        /// <summary>
        /// Weight Value
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_WEIGHT_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_WEIGHT_VAL"].AsInteger;
            }
        }

        /// <summary>
        /// IV Value
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_IV_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_IV_VAL"].AsInteger;
            }
        }

        /// <summary>
        /// IR Value
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_IR_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_IR_VAL"].AsInteger;
            }
        }

        /// <summary>
        /// Cold Press IR Value 1
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_COLDPRESS_IR_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_COLDPRESS_IR_VAL"].AsInteger;
            }
        }

        /// <summary>
        /// Thick1 Value 1
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_THIC1_SPEC_VAL1
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC1_SPEC_VAL1"].AsInteger;
            }
        }

        /// <summary>
        /// Thick1 Value 2
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_THIC1_SPEC_VAL2
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC1_SPEC_VAL2"].AsInteger;
            }
        }

        /// <summary>
        /// Thick1 Value 3
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_THIC1_SPEC_VAL3
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC1_SPEC_VAL3"].AsInteger;
            }
        }

        /// <summary>
        /// Thick1 Value 4
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_THIC1_SPEC_VAL4
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC1_SPEC_VAL4"].AsInteger;
            }
        }

        /// <summary>
        /// Thick Max Value
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_THIC_MAX_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC_MAX_VAL"].AsInteger;
            }
        }

        /// <summary>
        /// Thick Min Value
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G3_5_APD_RPT__I_W_THIC_MIN_VAL
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_THIC_MIN_VAL"].AsInteger;
            }
        }

        /// <summary>
        /// BCR Using
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G3_5_APD_RPT__I_B_BCR_USING
        {
            get
            {
                return this["G3_5_APD_RPT:I_B_BCR_USING"].AsBoolean;
            }
        }

        /// <summary>
        /// Reading Type
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G3_5_APD_RPT__I_W_READING_TYPE
        {
            get
            {
                return this["G3_5_APD_RPT:I_W_READING_TYPE"].AsShort;
            }
        }

        /// <summary>
        /// Bad Cell SSF OutPut Req
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_3_CELL_OUT_RPT__O_B_TRIGGER_REPORT_CONF
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:O_B_TRIGGER_REPORT_CONF"].AsBoolean;
            }
            set
            {
                this["G4_3_CELL_OUT_RPT:O_B_TRIGGER_REPORT_CONF"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Cell SSF Output Confirm Ack
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G4_3_CELL_OUT_RPT__O_W_TRIGGER_REPORT_ACK
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:O_W_TRIGGER_REPORT_ACK"].AsShort;
            }
            set
            {
                this["G4_3_CELL_OUT_RPT:O_W_TRIGGER_REPORT_ACK"].AsShort = value;
            }
        }

        /// <summary>
        /// Bad Cell SSF OutPut Req
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_3_CELL_OUT_RPT__I_B_TRIGGER_REPORT
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:I_B_TRIGGER_REPORT"].AsBoolean;
            }
        }

        /// <summary>
        /// Bad Cell SSF OutPut Exists
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_3_CELL_OUT_RPT__I_B_CELL_EXIST
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:I_B_CELL_EXIST"].AsBoolean;
            }
        }

        /// <summary>
        /// Bad Cell SSF Cell ID
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_3_CELL_OUT_RPT__I_W_CELL_ID
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:I_W_CELL_ID"].AsString;
            }
        }

        /// <summary>
        /// Bad Cell SSF OutPut Info
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_3_CELL_OUT_RPT__I_W_CELL_OUTPUT_INFO
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:I_W_CELL_OUTPUT_INFO"].AsInteger;
            }
        }

        /// <summary>
        /// Bad Cell SSF Judge
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_3_CELL_OUT_RPT__I_W_CELL_OUTPUT_JUDG
        {
            get
            {
                return this["G4_3_CELL_OUT_RPT:I_W_CELL_OUTPUT_JUDG"].AsInteger;
            }
        }

        /// <summary>
        /// Cell Info Req Conf
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_B_TRIGGER_REPORT_CONF"].AsBoolean;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_B_TRIGGER_REPORT_CONF"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Cell Info Req Ack
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G4_6_CELL_INFO_REQ__O_W_TRIGGER_REPORT_ACK
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_TRIGGER_REPORT_ACK"].AsShort;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_TRIGGER_REPORT_ACK"].AsShort = value;
            }
        }

        /// <summary>
        /// Input Lot ID
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ__O_W_LOT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_LOT_ID"].AsString;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_LOT_ID"].AsString = value;
            }
        }

        /// <summary>
        /// Input Group Lot ID
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ__O_W_GROUP_LOT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_GROUP_LOT_ID"].AsString;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_GROUP_LOT_ID"].AsString = value;
            }
        }

        /// <summary>
        /// Product ID 
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ__O_W_PRODUCT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_PRODUCT_ID"].AsString;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_PRODUCT_ID"].AsString = value;
            }
        }

        /// <summary>
        /// MODEL ID
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ__O_W_MODEL_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_MODEL_ID"].AsString;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_MODEL_ID"].AsString = value;
            }
        }

        /// <summary>
        /// Check Item
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G4_6_CELL_INFO_REQ__O_W_HOST_CHECKITEM
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_HOST_CHECKITEM"].AsShort;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_HOST_CHECKITEM"].AsShort = value;
            }
        }

        /// <summary>
        /// Tab Cutting Use1
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G4_6_CELL_INFO_REQ__O_W_HOST_TAB_CUT_USE1
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_HOST_TAB_CUT_USE1"].AsShort;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_HOST_TAB_CUT_USE1"].AsShort = value;
            }
        }

        /// <summary>
        /// Weight Spec Max
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_WEIGHT_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_WEIGHT_SPEC_MAX"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_WEIGHT_SPEC_MAX"].AsInteger = value;
            }
        }

        /// <summary>
        /// Weight Spec Min
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_WEIGHT_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_WEIGHT_SPEC_MIN"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_WEIGHT_SPEC_MIN"].AsInteger = value;
            }
        }

        /// <summary>
        /// Thick Spec Max
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_THIC_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_THIC_SPEC_MAX"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_THIC_SPEC_MAX"].AsInteger = value;
            }
        }

        /// <summary>
        /// Thick Spec Min
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_THIC_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_THIC_SPEC_MIN"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_THIC_SPEC_MIN"].AsInteger = value;
            }
        }

        /// <summary>
        /// Volt Spec Max
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_VOLT_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_VOLT_SPEC_MAX"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_VOLT_SPEC_MAX"].AsInteger = value;
            }
        }

        /// <summary>
        /// Volt Spec Min
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_VOLT_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_VOLT_SPEC_MIN"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_VOLT_SPEC_MIN"].AsInteger = value;
            }
        }

        /// <summary>
        /// ACIR Spec Max
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_ACIR_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_ACIR_SPEC_MAX"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_ACIR_SPEC_MAX"].AsInteger = value;
            }
        }

        /// <summary>
        /// ACIR Spec Min
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_ACIR_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_ACIR_SPEC_MIN"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_ACIR_SPEC_MIN"].AsInteger = value;
            }
        }

        /// <summary>
        /// IV Spec Max
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_IV_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_IV_SPEC_MAX"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_IV_SPEC_MAX"].AsInteger = value;
            }
        }

        /// <summary>
        /// IV Spec Min
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_IV_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_IV_SPEC_MIN"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_IV_SPEC_MIN"].AsInteger = value;
            }
        }

        /// <summary>
        /// Press OCV Spec Max
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_PRESS_OCV_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_PRESS_OCV_SPEC_MAX"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_PRESS_OCV_SPEC_MAX"].AsInteger = value;
            }
        }

        /// <summary>
        /// Press OCV Spec Min
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_PRESS_OCV_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_PRESS_OCV_SPEC_MIN"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_PRESS_OCV_SPEC_MIN"].AsInteger = value;
            }
        }

        /// <summary>
        /// IR Spec Max
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_IR_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_IR_SPEC_MAX"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_IR_SPEC_MAX"].AsInteger = value;
            }
        }

        /// <summary>
        /// IR Spec Min
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_IR_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_IR_SPEC_MIN"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_IR_SPEC_MIN"].AsInteger = value;
            }
        }

        /// <summary>
        /// Cold Press IR Spec Max
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_COLDPRESS_IR_SPEC_MAX
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_COLDPRESS_IR_SPEC_MAX"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_COLDPRESS_IR_SPEC_MAX"].AsInteger = value;
            }
        }

        /// <summary>
        /// Cold Press IR Spec Min
        ///  (Access Type : Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ__O_W_COLDPRESS_IR_SPEC_MIN
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:O_W_COLDPRESS_IR_SPEC_MIN"].AsInteger;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ:O_W_COLDPRESS_IR_SPEC_MIN"].AsInteger = value;
            }
        }

        /// <summary>
        /// Cell Info Req
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_6_CELL_INFO_REQ__I_B_TRIGGER_REPORT
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:I_B_TRIGGER_REPORT"].AsBoolean;
            }
        }

        /// <summary>
        /// Input Cell_ID1
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ__I_W_CELL_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:I_W_CELL_ID"].AsString;
            }
        }

        /// <summary>
        /// Input Cell Exists
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_6_CELL_INFO_REQ__I_B_CELL_EXIST
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:I_B_CELL_EXIST"].AsBoolean;
            }
        }

        /// <summary>
        /// BCR Using
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_6_CELL_INFO_REQ__I_B_BCR_USING
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:I_B_BCR_USING"].AsBoolean;
            }
        }

        /// <summary>
        /// Reading Type
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G4_6_CELL_INFO_REQ__I_W_READING_TYPE
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ:I_W_READING_TYPE"].AsShort;
            }
        }

        /// <summary>
        /// 2D Cell Info Req Conf
        ///  (Access Type : Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_6_CELL_INFO_REQ_03__O_B_TRIGGER_REPORT_CONF
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_B_TRIGGER_REPORT_CONF"].AsBoolean;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ_03:O_B_TRIGGER_REPORT_CONF"].AsBoolean = value;
            }
        }

        /// <summary>
        /// 2DCell Info Req Ack
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G4_6_CELL_INFO_REQ_03__O_W_TRIGGER_REPORT_ACK
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_TRIGGER_REPORT_ACK"].AsShort;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ_03:O_W_TRIGGER_REPORT_ACK"].AsShort = value;
            }
        }

        /// <summary>
        /// Input Lot ID
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ_03__O_W_LOT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_LOT_ID"].AsString;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ_03:O_W_LOT_ID"].AsString = value;
            }
        }

        /// <summary>
        /// Input Group Lot ID
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ_03__O_W_GROUP_LOT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_GROUP_LOT_ID"].AsString;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ_03:O_W_GROUP_LOT_ID"].AsString = value;
            }
        }

        /// <summary>
        /// Product ID
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ_03__O_W_PRODUCT_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_PRODUCT_ID"].AsString;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ_03:O_W_PRODUCT_ID"].AsString = value;
            }
        }

        /// <summary>
        /// MODEL ID
        ///  (Access Type : Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ_03__O_W_MODEL_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_MODEL_ID"].AsString;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ_03:O_W_MODEL_ID"].AsString = value;
            }
        }

        /// <summary>
        /// 2D_CELL_JUDGE
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G4_6_CELL_INFO_REQ_03__O_W_HOST_CELL_2D_JUDG
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_HOST_CELL_2D_JUDG"].AsShort;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ_03:O_W_HOST_CELL_2D_JUDG"].AsShort = value;
            }
        }

        /// <summary>
        /// GBT_CELL_JUDGE
        ///  (Access Type : Out, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G4_6_CELL_INFO_REQ_03__O_W_HOST_CELL_GBT_JUDG
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:O_W_HOST_CELL_GBT_JUDG"].AsShort;
            }
            set
            {
                this["G4_6_CELL_INFO_REQ_03:O_W_HOST_CELL_GBT_JUDG"].AsShort = value;
            }
        }

        /// <summary>
        /// 2D Cell Info Req
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_6_CELL_INFO_REQ_03__I_B_TRIGGER_REPORT
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_B_TRIGGER_REPORT"].AsBoolean;
            }
        }

        /// <summary>
        /// 2D GBT Cell_ID, 
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ_03__I_W_CELL_ID
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_CELL_ID"].AsString;
            }
        }

        /// <summary>
        /// 2D Cell Exists
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_6_CELL_INFO_REQ_03__I_B_CELL_EXIST
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_B_CELL_EXIST"].AsBoolean;
            }
        }

        /// <summary>
        /// 2D Verify GRADE
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ_03__I_W_2D_VERIFY_GD
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_2D_VERIFY_GD"].AsString;
            }
        }

        /// <summary>
        /// 2D Verify Data
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ_03__I_W_2D_VERIFY_DATA
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_2D_VERIFY_DATA"].AsString;
            }
        }

        /// <summary>
        /// GBT Verify Length
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ_03__I_W_2D_VERIFY_LENGTH
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_2D_VERIFY_LENGTH"].AsInteger;
            }
        }

        /// <summary>
        /// GBT Verify GRADE
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ_03__I_W_GBT_VERIFY_GD
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_GBT_VERIFY_GD"].AsString;
            }
        }

        /// <summary>
        /// GBT Verify Data
        ///  (Access Type : In, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string G4_6_CELL_INFO_REQ_03__I_W_GBT_VERIFY_DATA
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_GBT_VERIFY_DATA"].AsString;
            }
        }

        /// <summary>
        /// GBT Verify Length
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ_03__I_W_GBT_VERIFY_LENGTH
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_GBT_VERIFY_LENGTH"].AsInteger;
            }
        }

        /// <summary>
        /// Print Mode
        ///  (Access Type : In, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int G4_6_CELL_INFO_REQ_03__I_W_PRINT_MODE1
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_PRINT_MODE1"].AsInteger;
            }
        }

        /// <summary>
        /// BCR Using
        ///  (Access Type : In, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool G4_6_CELL_INFO_REQ_03__I_B_BCR_USING
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_B_BCR_USING"].AsBoolean;
            }
        }

        /// <summary>
        /// Reading Type
        ///  (Access Type : In, Data Type : ushort)
        /// </summary>
        [Browsable(false)]
        public ushort G4_6_CELL_INFO_REQ_03__I_W_READING_TYPE
        {
            get
            {
                return this["G4_6_CELL_INFO_REQ_03:I_W_READING_TYPE"].AsShort;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool _PLC03___IS_CONNECTED
        {
            get
            {
                return this["[PLC03]:IS_CONNECTED"].AsBoolean;
            }
            set
            {
                this["[PLC03]:IS_CONNECTED"].AsBoolean = value;
            }
        }
        #endregion
    }
}