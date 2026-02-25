using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

using LGCNS.ezControl.Common;
using LGCNS.ezControl.Core;
using LGCNS.ezControl.Diagnostics;
using LGCNS.ezControl.Data;

using SolaceSystems.Solclient.Messaging;
using Newtonsoft.Json;
using System.Reflection;

namespace ESHG.EIF.FORM.EOLPACKER
{
    public partial class CEOLPACKER_BIZ : CImplement, IEIF_Biz
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void SetLocalTime([In] ref DBTIME lpSystemTime);

        [DllImport("kernel32.dll")]
        static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);

        #region [ Field ]
        protected string strLogCategory = string.Empty;
        protected string strLangID = string.Empty;

        private bool bSendDateTime = false;
        private int iSendDateTimeday = Convert.ToInt16(DateTime.Now.Day) - 1;

        protected string strEqpID = string.Empty;
        protected bool bTestMode = false;
        protected string strMachineID = string.Empty;

        private const int MAX_HOST_ALARM_MSG = 8;

        string _logPath = string.Empty;

        #region Simulation Mode 설정 관련
        public const Boolean SIMULATION_MODE = false; //$ 2025.01.21 : 사전 검수 모드는 빌드를 통해서만 바꿀수 있게 하자        
        #endregion

        public int TIMEOUTSEC { get; set; } = 10;
        public int SCANINTERVAL { get; set; } = 500;
        public int NSECINTERVAL { get; set; } = 3000;

        private string ReqQueue { get { return this.ADDINFO__V_REQQUEUE_NAME; } }
        private string RepQueue { get { return $"REPLY/{ReqQueue}"; } }

        public int BizTimeout { get { return this.ADDINFO__V_BIZCALL_TIMEOUT; } }

        protected CPACKERUNIT UNIT { get { return (Owner as CPACKERUNIT); } }

        protected CPACKER BASE { get { return (Owner.Parent as CPACKER); } }

        //$ 2025.06.12 : Packer는 기존 활성화와 다르게 Control Server는 껍데이기이고, 실제 IO는 Unit Element에 정의됨. 모델러 Element 등록시 반드시 UNIT Keyword 포함해야 함
        //protected override CElement Owner => CExecutor.ElementsByElementPath["ESHG.PACKER.IO.ESHG.PACKER.UNIT"];
        protected override CElement Owner => CExecutor.ElementsByElementPath.Where(r => r.Key.Contains("UNIT")).First().Value;

        //JH 2025.02.08 : 이전 Disconnect 여부를 체크할 수 있도록 Property 추가 [UC2 내역 반영]
        private bool IsBeforeDisconnected { get; set; }

        //$ 2024.02.29 : 사전 검수 테스트시 NG나 Timeout을 1회씩 발생시키기 위해 변수 선언
        protected Dictionary<string, bool> NakPassList = null;
        protected Dictionary<string, bool> TimeOutPassList = null;

        protected Dictionary<string, int> dicRptPstnCode = new Dictionary<string, int>();

        public Dictionary<string, CReference> _ReferenceProcess = new Dictionary<string, CReference>();
        public Dictionary<string, string> _dicPortAgvInfo = new Dictionary<string, string>();

        protected List<CVariable> listReqeustVariable = new List<CVariable>();
        protected List<CVariable> listConfirmVariable = new List<CVariable>();

        Dictionary<int, string> _strlstApdData = new Dictionary<int, string>();
        Dictionary<int, int> _ilstApdDataFPoint = new Dictionary<int, int>();

        public Dictionary<int, CEioState> _dicEioStateStnd = new Dictionary<int, CEioState>();
        public Dictionary<int, CClctItem> _dicCLCTITEM = new Dictionary<int, CClctItem>();

        private object objRemoteCommandLock = new object();
        private object objLockGetSystemTime = new object();
        private object objLockGetHostErrMsg = new object();
        private object objLockGetEqptOpMode = new object();
        private object objLockRegEioState = new object();
        private object objLockRegAlarmSet = new object();  //$ 2023.02.03
        private object objLockRegAlarmReset = new object();  //$ 2023.02.03
        private object objLockRegSmokeDetect = new object();  //JH 2024.07.23
        private object objLockRegEqptDataClct = new object();
        private object objLockRegWipDataClct = new object();
        private object objLockEqptWipQty = new object();
        private object objLockRegEqptDefectClct = new object();
        private object objLockRegBizRuleErr = new object();
        private object objLockOperInfo = new object();

        private object _lockHostAlm = new object();
        #endregion

        #region [ FactovaLync Method Override ]

        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            __INTERNAL_VARIABLE_BOOLEAN("V_IS_SIXLOSSCODE_USE", "EQPINFO", enumAccessType.Virtual, true, false, false, "", "True - Loss Code 6자리 사용, False - 기존 처럼 3자리 사용"); //$ 2023.07.26 : Loss Code 3자리 or 6자리 사용 여부

            __INTERNAL_VARIABLE_STRING("V_REQQUEUE_NAME", "ADDINFO", enumAccessType.Virtual, false, true, "", "", "EIF -> Biz Server Req Queue Name");
            __INTERNAL_VARIABLE_INTEGER("V_BIZCALL_TIMEOUT", "ADDINFO", enumAccessType.Virtual, 0, 0, true, false, 30000, string.Empty, "Biz Call TimeOut(mSec)");

            #region Factova Monitoring용 가상 변수
            __INTERNAL_VARIABLE_STRING("V_MONITOR_FACTORY", "MONITOR", enumAccessType.Virtual, false, true, "ESHG_EIF", "", "설비 공장명");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_CATEGORY", "MONITOR", enumAccessType.Virtual, false, true, "Form", "", "설비 카테고리");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_EQUIPMENT_ID", "MONITOR", enumAccessType.Virtual, false, true, "", "", "설비 ID");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_EQP_NICNAME", "MONITOR", enumAccessType.Virtual, false, true, "", "", "장비 NIC Name");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_DEVICETYPE", "MONITOR", enumAccessType.Virtual, false, true, "EIF", "", "설비 장비타입");

            __INTERNAL_VARIABLE_STRING("V_MONITOR_HOST_COMMUNICATIONSTATE", "MONITOR", enumAccessType.Virtual, false, true, "", "", "Host와의 통신 상태 Online,Offline");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_PLC_COMMNUICATION", "MONITOR", enumAccessType.Virtual, false, false, "", "", "PLC와의 통신 상태");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_CIMSTATUS", "MONITOR", enumAccessType.Virtual, false, true, "", "", "CIM Online Status 상태 Auto,Pausing,Paused..Reconcileing");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_BIZ_VERSION", "MONITOR", enumAccessType.Virtual, false, true, "", "", "Biz Version");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_SCAN_INTERVAL", "MONITOR", enumAccessType.Virtual, false, true, "", "", "Scan Interval");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_BASE_HOSTNAME", "MONITOR", enumAccessType.Virtual, false, true, "", "", "MCCS 운영 기준 ENG01 또는 ENG02 의 Base HostName. 어떤 Node에서 운영중인지 확인 용도");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_NOTIFICATION", "MONITOR", enumAccessType.Virtual, false, true, "", "", "통합관리로 Risk 정보를 Notification");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_LOCAL_HOST_IP", "MONITOR", enumAccessType.Virtual, false, true, "", "", "MCS HSMS driver의 Local Host IP (virtual IP)");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_SOLACE", "MONITOR", enumAccessType.Virtual, false, false, "", "", "Solace 접속 상태");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_FACTOVA_VER", "MONITOR", enumAccessType.Virtual, false, true, "", "", "Factova Version");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_EQPSTATUS", "MONITOR", enumAccessType.Virtual, false, false, "", "", "EQP Status Run, Wait, Trouble, User Stop");
            #endregion
        }

        protected override void OnInitializeCompleted()
        {
            base.OnInitializeCompleted();

            string[] lstLog = ((LGCNS.ezControl.Diagnostics.Loggers.FileLogger)this.Logger.Children[1]).FilePath.Split('\\');

            for (int i = 0; i <= 3; i++)
            {
                if (i > 0) _logPath += "\\";
                _logPath += lstLog[i];
            }

            this.MONITOR__V_MONITOR_EQUIPMENT_ID = strEqpID;
            this.MONITOR__V_MONITOR_EQP_NICNAME = this.BASE.Description;  //$ 2025.10.31 : NICKNAEM 항목에 ControlServer Desctripion을 보여 주기로 함
            this.MONITOR__V_MONITOR_HOST_COMMUNICATIONSTATE = CommunicationState.ONLINE.ToString();
            this.MONITOR__V_MONITOR_BIZ_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.MONITOR__V_MONITOR_BASE_HOSTNAME = Environment.MachineName;
            this.MONITOR__V_MONITOR_LOCAL_HOST_IP = GetHostIP(this.BASE.Variables["SOLACE:CONNECTION_INFO"].ToString());
            this.MONITOR__V_MONITOR_SOLACE = this.BASE.ConnectionState.ToString();
            this.MONITOR__V_MONITOR_FACTOVA_VER = Assembly.GetEntryAssembly().GetName().Version.ToString();
            this.MONITOR__V_MONITOR_SCAN_INTERVAL = string.Join(",", this.BASE.Drivers[1].ScanInterval);
        }

        protected override void OnStarted()
        {
            base.OnStarted();

            try
            {
                foreach (CDriver drv in this.BASE.Drivers.Values)
                {
                    if (drv.Name.Contains(Name))
                    {
                        if (drv.ConnectionState == LGCNS.ezControl.Common.enumConnectionState.Connected)
                        {
                            this.UNIT.V_DRIVER_CONNECTED = true;
                            EIFLog(Level.Verbose, $"[OnStarted] {drv.Name.ToString()} : Connected\r\n", strLogCategory, false, strEqpID, SHOPID.FORM);
                        }
                        else
                        {
                            this.UNIT.V_DRIVER_CONNECTED = false;
                            EIFLog(Level.Verbose, $"[OnStarted] {drv.Name.ToString()} : {drv.ConnectionState.ToString()}\r\n", strLogCategory, false, strEqpID, SHOPID.FORM);
                        }
                        drv.ConnectionStateChanged += new delegateDriverConnectionStateChanged(DriverConnectionStateChanged);
                        drv.ErrorOccurred += new delegateDriverErrorOccurred(DriverErrorOccurred);
                    }
                }
                GuiLanguageTypeChanged(this.UNIT.__EQP_OP_MODE_CHG_RPT__I_W_HMI_LANG_TYPE.AsShort);

                EIFMonitoringData();

                //WipRWTDataReport(); //$ 2025.09.22 : Scheduler Interval 이후 호출 되는 것이 문제가 된다면 OnStarted에서 명시적으로 호출 후 이후 Interval대로 반복 호출 함
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        protected override void DefineHandlers()
        {
            base.DefineHandlers();

            try
            {
                __EVENT_ON(this.UNIT.Variables["ADDINFO:O_B_RELOAD_VARIABLE"], ADDINFO_O_B_RELOAD_VARIABLE_OnVariableOn);

                #region 7.1.1.1 [C1-1] EQP Communication Check
                __EVENT_ON(this.UNIT.__HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF, HostCommunicationCheck);
                #endregion

                #region 7.1.1.3 [C1-3] Communication State Change Report
                __EVENT_BOOLEANCHANGED(this.UNIT.__COMM_STAT_CHG_RPT__I_B_COMM_ON, CommunicationStateChangeReport);
                __EVENT_BOOLEANCHANGED(this.UNIT.__COMM_STAT_CHG_RPT__I_B_COMM_OFF, CommunicationStateChangeReport);
                #endregion

                #region 7.1.1.4 [C1-4] Date and Time Set Request
                __EVENT_BOOLEANCHANGED(this.UNIT.__DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ, DateAndTimeSetReqest);
                #endregion

                #region 7.1.2.1 [C2-1] Equipment State Change Report
                __EVENT_SHORTCHANGED(this.UNIT.__EQP_STAT_CHG_RPT__I_W_EQP_STAT, EquipmentStateChangeReport, true);
                __EVENT_SHORTCHANGED(this.UNIT.__EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT, EquipmentStateChangeReport, true);
                #endregion

                #region 7.1.2.2 [C2-2] Alarm Report
                #endregion

                #region 7.1.2.3 [C2-3] Host Alarm Message Send

                #endregion

                #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report
                __EVENT_BOOLEANCHANGED(this.UNIT.__EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE, OperationModeChanged);
                __EVENT_BOOLEANCHANGED(this.UNIT.__EQP_OP_MODE_CHG_RPT__I_B_IT_BYPASS, ITBypassChanged);
                __EVENT_SHORTCHANGED(this.UNIT.__EQP_OP_MODE_CHG_RPT__I_W_HMI_LANG_TYPE, HMILangTypeChanged, true);
                #endregion

                #region 7.1.2.5 [C2-5] Processing State Change Report
                #endregion

                #region 7.1.2.6 [C2-6] Remote Command Send
                __EVENT_BOOLEANCHANGED(this.UNIT.__REMOTE_COMM_SND__O_B_REMOTE_COMMAND_SEND, RemoteCommandSend);
                __EVENT_BOOLEANCHANGED(this.UNIT.__REMOTE_COMM_SND__I_B_REMOTE_COMMAND_CONF, RemoteCommandConfirm);
                #endregion

                #region 7.1.2.8 [C2-8] Alarm Set Report
                __EVENT_BOOLEANCHANGED(this.UNIT.__ALARM_RPT__I_B_ALARM_SET_REQ, AlarmSetRequest);
                __EVENT_BOOLEANCHANGED(this.UNIT.__ALARM_RPT__O_B_ALARM_SET_CONF, HostConfirmBitOff);
                #endregion

                #region 7.1.2.9 [C2-9] Alarm Reset Report
                __EVENT_BOOLEANCHANGED(this.UNIT.__ALARM_RPT__I_B_ALARM_RESET_REQ, AlarmResetRequest);
                __EVENT_BOOLEANCHANGED(this.UNIT.__ALARM_RPT__O_B_ALARM_RESET_CONF, HostConfirmBitOff);
                #endregion

                #region 7.1.2.10 [C2-10] Smoke Detect Report
                __EVENT_BOOLEANCHANGED(this.UNIT.__SMOKE_RPT__I_B_SMOKE_DETECT_REQ, SmokeDetectReport);
                __EVENT_BOOLEANCHANGED(this.UNIT.__SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF, HostConfirmBitOff);
                #endregion

                __EVENT_STRINGCHANGED(this.BASE.Variables["BASICINFO:V_EQP_ID_01"], EqpIDChanged);

                foreach (CVariable vari in this.UNIT.Variables.Values)
                {
                    if (vari.Name.StartsWith(CTag.I_B_TRIGGER_REPORT))
                    {
                        __EVENT_ON(vari, EQP_Trigger_OnVariableOn);

                        if (this.UNIT.Variables.ContainsKey(vari.Category.Name + ":" + CTag.O_B_TRIGGER_REPORT_CONF))
                        {
                            listReqeustVariable.Add(vari);
                            listConfirmVariable.Add(this.UNIT.Variables[vari.Category.Name + ":" + CTag.O_B_TRIGGER_REPORT_CONF]);
                        }
                    }
                    else if (vari.Name.StartsWith(CTag.I_W_TRIGGER_) || vari.Name.StartsWith(CTag.I_B_TRIGGER_))
                    {
                        enumDataType varType = vari.DataType;

                        //$ 2025.04.24 : 기존 Object Type으로 발생되던 Event Method가 삭제되어 하기와 같이 세분화 하여 구현
                        if (varType == enumDataType.Boolean)
                            __EVENT_BOOLEANCHANGED(vari, EQP_Trigger_OnVariableChanged);
                        else if (varType == enumDataType.Short)
                            __EVENT_SHORTCHANGED(vari, EQP_Trigger_OnVariableChanged, true);
                        else if (varType == enumDataType.SignedShort)
                            __EVENT_SIGNEDSHORTCHANGED(vari, EQP_Trigger_OnVariableChanged, true);
                        else if (varType == enumDataType.Integer)
                            __EVENT_INTEGERCHANGED(vari, EQP_Trigger_OnVariableChanged);
                        else if (varType == enumDataType.String)
                            __EVENT_STRINGCHANGED(vari, EQP_Trigger_OnVariableChanged);
                    }
                    else if (vari.Name.StartsWith($"{CTag.O_B_HOST_TRIGGER_REPORT}"))
                    {
                        __EVENT_BOOLEANCHANGED(vari, HOST_Trigger_OnBooleanChanged);
                    }
                }

                __EVENT_BOOLEANCHANGED(this.UNIT.Variables["ADDINFO:V_DRY_RUN_NGTIMEOUT"], ADD_INFO__V_DRY_RUN_NGTIMEOUT_OnBooleanChanged); //$ 2024.02.29 : NG/Timout 설정이 Off될 때 Dictionary 초기화

                //$ 2025.05.21 : ManagedFunction -> Scheduler로 변경, 기본적인 모델러의 Interval 설정이 ms단위로 되어 있어 * 1000을 해줄 필요가 없음 
                //$ 2025.07.26 : 초기 Scheduler 시작 시간을 짧게 설정하고 해당 Scheduler 함수 안에서 Wait로 시간 조정
                //$ 2025.09.22 : 기존 100ms 후 빠르게 Scheduler 함수 호출하고 Wait로 Interval 조정하던 것을 정상적인 Process로 처리(프로그램 시작하자마 호출 필요 시 따로 Scheduler 함수 호출)
                __SCHEDULER(EquipmentCommunicationCheck, this.UNIT.BASICINFO__V_COM_CHECK_INTERVAL, true);
                __SCHEDULER(DoModelIDReport, this.UNIT.BASICINFO__V_WIP_DATA_RPT_INTERVAL, true);
                __SCHEDULER(WipRWTDataReport, this.UNIT.BASICINFO__V_WIP_DATA_RPT_INTERVAL, true);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        protected override void OnInstancingCompleted()
        {
            base.OnInstancingCompleted();

            this.BASE.HandleEmptyStringByNull = true; //$ 2024.11.11 : Biz Mapping안하면 Null로 보고

            OnInstancingCompleted_DeviceType();

            // this.BASE.__V_DRIVER_CONNECTED.BroadcastIfValueChanged = true;     //$ 2025.04.23 : 임시로 주석 처리해둠....
            strEqpID = this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString.Trim();
            strMachineID = this.UNIT.Variables["BASICINFO:V_EQP_MACHINE_ID_01"].AsString.Trim();

            strLogCategory = this.Name.Trim();

            LoadCLCTITEM();

            LoadEioStateStnd();

            __MONITOR__V_MONITOR_FACTORY.SystemMonitoringInfo = new CVariableSystemMonitoringInfo() { CategoryLevel = 0, IsCategoryItem = true };
            __MONITOR__V_MONITOR_CATEGORY.SystemMonitoringInfo = new CVariableSystemMonitoringInfo() { CategoryLevel = 1, IsCategoryItem = true };

            __MONITOR__V_MONITOR_CATEGORY.SystemMonitoring = true;      //Modeler에서 입력
            __MONITOR__V_MONITOR_FACTORY.SystemMonitoring = true;       //Modeler에서 입력

            __MONITOR__V_MONITOR_EQUIPMENT_ID.SystemMonitoring = true;  // CImp에서
            __MONITOR__V_MONITOR_EQP_NICNAME.SystemMonitoring = true;   // ??
            __MONITOR__V_MONITOR_DEVICETYPE.SystemMonitoring = true;    //Modeler에서 입력

            __MONITOR__V_MONITOR_HOST_COMMUNICATIONSTATE.SystemMonitoring = true; // CImp에서 EIF가 켜지면 1, 꺼지면 0
            __MONITOR__V_MONITOR_PLC_COMMNUICATION.SystemMonitoring = true;       // Common에서 PLC 연결 상태 보고 1, 0
            __MONITOR__V_MONITOR_CIMSTATUS.SystemMonitoring = false;               // ??CIM이 없는데.

            __MONITOR__V_MONITOR_BIZ_VERSION.SystemMonitoring = true;              //CImp에서
            __MONITOR__V_MONITOR_SCAN_INTERVAL.SystemMonitoring = true;            //CImp에서

            __MONITOR__V_MONITOR_BASE_HOSTNAME.SystemMonitoring = true;             //CImp에서
            __MONITOR__V_MONITOR_NOTIFICATION.SystemMonitoring = true;              //??

            __MONITOR__V_MONITOR_LOCAL_HOST_IP.SystemMonitoring = true;
            __MONITOR__V_MONITOR_SOLACE.SystemMonitoring = true;
            __MONITOR__V_MONITOR_FACTOVA_VER.SystemMonitoring = true;
            __MONITOR__V_MONITOR_EQPSTATUS.SystemMonitoring = true;
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            this.MONITOR__V_MONITOR_SOLACE = enumConnectionState.Disconnected.ToString();
            this.MONITOR__V_MONITOR_HOST_COMMUNICATIONSTATE = CommunicationState.OFFLINE.ToString();
        }
        #endregion

        #region Solace Event Method   
        public void OnMessageReceived(IMessage request, string topic, string message)
        {
            string rcvdAlarmMsg = string.Empty;
            Exception Ex = new Exception("Host Alarm Message Received");
            string LangID = GLOBAL_LANGUAGE_SET.ENGLISH;

            try
            {
                HOSTMSG_SEND msg = JsonConvert.DeserializeObject<HOSTMSG_SEND>(message);

                //KeyValuePair<string, CElement>[] arrElement = Elements.ToArray();

                //for (int i = 0; i < arrElement.Count(); i++)
                {
                    //CImplement objEqp = arrElement[i].Value as CImplement;

                    //if (objEqp == null) continue;

                    // check EQP ID
                    string eqpID = this.BASE.Variables.ContainsKey("BASICINFO:V_EQP_ID_01") ? this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString : "";
                    if (eqpID != msg.refDS.IN_DATA[0].EQPTID)
                    {
                        Logger.Log(Level.Debug, $"[{"RCVDMSG",7}] [Received EQPID is not valid.]  Message: {message}", this.Name, this.Name);
                        return;
                    }

                    int iLangType = this.UNIT.Variables.ContainsKey("EQP_OP_MODE_CHG_RPT:I_W_HMI_LANG_TYPE") ? this.UNIT.Variables["EQP_OP_MODE_CHG_RPT:I_W_HMI_LANG_TYPE"].AsShort : 0;

                    switch (iLangType)
                    {
                        case GLOBAL_LANGUAGE.KOREA:
                            rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_KOR_1;
                            LangID = GLOBAL_LANGUAGE_SET.KOREA;
                            break;
                        case GLOBAL_LANGUAGE.ENGLISH:
                            rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_ENG_1;
                            LangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                            break;
                        case GLOBAL_LANGUAGE.CHINA:
                            rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_CHN_1;
                            LangID = GLOBAL_LANGUAGE_SET.CHINA;
                            break;
                        case GLOBAL_LANGUAGE.POLAND:
                            rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_POL_1;
                            LangID = GLOBAL_LANGUAGE_SET.POLAND;
                            break;
                        case GLOBAL_LANGUAGE.UKRAINE:
                            rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_UKR_1;
                            LangID = GLOBAL_LANGUAGE_SET.UKRAINE;
                            break;
                        case GLOBAL_LANGUAGE.RUSSIA:
                            rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_RUS_1;
                            LangID = GLOBAL_LANGUAGE_SET.RUSSIA;
                            break;
                        default:
                            rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_ENG_1;
                            LangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                            break;
                    }


                    // Send System
                    ushort sendSystem = 0;
                    switch (msg.refDS.IN_DATA[0].SYS_NAME)
                    {
                        case "APC":
                            sendSystem = 1;
                            break;
                        case "FDC":
                            sendSystem = 2;
                            break;
                        case "SPC+":
                            sendSystem = 3;
                            break;
                        default:
                            sendSystem = 0;     // MES
                            break;
                    }

                    // Host Alarm Action (stop type)
                    ushort stop = Convert.ToUInt16(msg.refDS.IN_DATA[0].STOP_TYPE);

                    // Send Alarm
                    SendHostAlarmMsg(Ex, LangID, HOST_ALM_TYPE.COMM_TYPE, sendSystem, stop, strHostAlarmMsg: rcvdAlarmMsg);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }
        #endregion

        #region [ IO Event Method ]
        #region Common
        private void DriverConnectionStateChanged(CDriver driver, enumConnectionState connectionState)
        {
            try
            {
                if (driver.Name.Equals(Name))
                {
                    switch (connectionState)
                    {
                        case enumConnectionState.Connected:
                            this.UNIT.V_DRIVER_CONNECTED = true;
                            EIFLog(Level.Verbose, $"[Driver Connection State Changed] {driver.Name.ToString()} : Connected\r\n", strLogCategory, false, strEqpID, SHOPID.FORM);
                            SendEquipmentState(true);

                            if (this.IsBeforeDisconnected)
                            {
                                // JH 2025.02.08 : Driver 연결 시 동기화 관련 Packer 프로세스 추가(**중요: Driver 설정시 Driver  name을 Factova Modeler에 설정된 Packer Unit이름으로 해야함) [UC2] 내역 반영
                                DateTime dateNow = DateTime.Now;
                                List<ushort> SystemTime_Now = new ushort[] { (ushort)dateNow.Year, (ushort)dateNow.Month, (ushort)dateNow.Day, (ushort)dateNow.Hour, (ushort)dateNow.Minute, (ushort)dateNow.Second }.ToList();

                                this.UNIT.DATE_TIME_SET_REQ__O_W_DATE_TIME = SystemTime_Now;
                                this.UNIT.DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ = true;

                                this.IsBeforeDisconnected = false;
                            }

                            break;

                        case enumConnectionState.Disconnected:
                            this.UNIT.V_DRIVER_CONNECTED = false;
                            EIFLog(Level.Verbose, $"[Driver Connection State Changed] {driver.Name.ToString()} : Disconnected\r\n", strLogCategory, false, strEqpID, SHOPID.FORM);
                            SendEquipmentState(false);

                            this.IsBeforeDisconnected = true;
                            break;

                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        private void DriverErrorOccurred(CDriver driver, int iErrorCode)
        {
            try
            {
                this.UNIT.V_DRIVER_CONNECTED = false;
                EIFLog(Level.Verbose, $"DrvErrorOccurred : {iErrorCode}\r\n", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        private void EqpIDChanged(CVariable sender, string value)
        {
            strEqpID = this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString.Trim();
        }

        private void EQP_Trigger_OnVariableOn(CVariable sender)
        {
            string strLogCat = strEqpID;
            bool bForcedTimeOut = false;

            string txnID = this.GenerateTransactionKey();

            try
            {
                SOLLog(Level.Info, $"[STEP_1] {sender.NameCategorized} : On", strLogCategory, strLogCat, SHOPID.FORM, txnID);
                EIFLog(Level.Verbose, $"{sender.NameCategorized} : On", strLogCategory, false, strLogCat, SHOPID.FORM);

                switch (sender.Category.Name)
                {
                    default:
                        if (!this.UNIT.ADDINFO__V_DRY_RUN)
                        {
                            int iRet = DispatchEvent(sender, txnID);

                            if (this.UNIT.Variables.ContainsKey(sender.Category.Name + ":" + CTag.O_W_TRIGGER_REPORT_ACK))
                            {
                                switch (iRet)
                                {
                                    case 0:
                                        this.UNIT.Variables[sender.Category.Name + ":" + CTag.O_W_TRIGGER_REPORT_ACK].AsShort = ConfirmAck.OK;
                                        SOLLog(Level.Info, $"[STEP_6] {sender.Category.Name + ":" + CTag.O_W_TRIGGER_REPORT_ACK} : 10", strLogCategory, strLogCat, SHOPID.FORM, txnID);
                                        break;

                                    default:
                                        this.UNIT.Variables[sender.Category.Name + ":" + CTag.O_W_TRIGGER_REPORT_ACK].AsShort = ConfirmAck.NG;
                                        SOLLog(Level.Warning, $"[STEP_6] {sender.Category.Name + ":" + CTag.O_W_TRIGGER_REPORT_ACK} : 11", strLogCategory, strLogCat, SHOPID.FORM, txnID);
                                        break;
                                }
                            }
                            GetVariableValueLog(sender, iRet);
                        }
                        else
                        {
                            foreach (CVariable vari in this.UNIT.Variables.Values.Where((p) => p.Category.Name == sender.Category.Name && p.AccessType == enumAccessType.In))
                            {
                                string Value = vari.Value.ToString(); // Variable Reload
                            }

                            if (this.UNIT.Variables["ADDINFO:V_DRY_RUN_BIZ_CONNECT"].AsBoolean) DispatchEvent(sender, txnID);

                            //$ 2024.02.29 : 테스트 모드시 OK와 NG를 Method로 따로 선언하여 코드 정리
                            if (this.UNIT.ADDINFO__V_DRY_RUN_OK)
                            {
                                if (!this.UNIT.ADDINFO__V_DRY_RUN_DIRECT_INPUT)
                                {
                                    HostTestModeOKReport(sender, txnID, strLogCat);
                                }
                            }
                            else if (this.UNIT.Variables["ADDINFO:V_DRY_RUN_NGTIMEOUT"].AsBoolean) //$ 2024.02.29 : V_DRY_RUN과 V_DRY_RUN_NGTIMEOUT이 같이 On되어 있는 경우 한 Event에 대해 NG -> Timeout -> OK 순으로 동작
                            {
                                #region NG -> Timeout -> OK 모드
                                string eventName = sender.Category.Name;

                                // NG 및 Timeout Skip Dictionary에 메모리 할당
                                if (this.NakPassList == null || this.NakPassList.Count == 0)
                                    this.NakPassList = new Dictionary<string, bool>();

                                if (!this.NakPassList.ContainsKey(eventName))
                                    this.NakPassList.Add(eventName, false);

                                if (this.TimeOutPassList == null || this.TimeOutPassList.Count == 0)
                                    this.TimeOutPassList = new Dictionary<string, bool>();

                                if (!this.TimeOutPassList.ContainsKey(eventName))
                                    this.TimeOutPassList.Add(eventName, false);


                                // 1. NG가 발생한 적 없으면 해당 Event에 NG Flag 살려 주고 NG 발생 시킴
                                if (this.NakPassList[eventName] == false)
                                {
                                    this.NakPassList[eventName] = true;

                                    HostTestModeNGReport(sender, txnID, strLogCat);
                                }
                                // 2. Time 발생한 적 없으면 해당 Event에 Timeout Flag 살려 주고 finally로 보내서 기존 Timout 발생 Logic 진행
                                else if (this.TimeOutPassList[eventName] == false)
                                {
                                    this.TimeOutPassList[eventName] = true;
                                    bForcedTimeOut = true; //이걸 해야 Bit를 On안하고 넘어감
                                }
                                // 3. NG, Timeout 한번씩 발생하고 난 다음에는 정상 Logic 반영해서 정상 진행하게 함
                                else
                                {
                                    HostTestModeOKReport(sender, txnID, strLogCat);
                                }
                                #endregion
                            }
                            else
                            {
                                HostTestModeNGReport(sender, txnID, strLogCat);
                            }
                            GetVariableValueLog(sender, this.UNIT.ADDINFO__V_DRY_RUN_OK == true ? 0 : -1);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strLogCat, SHOPID.FORM);
            }
            finally
            {
                if (this.UNIT.Variables.ContainsKey(sender.Category.Name + ":" + CTag.O_B_TRIGGER_REPORT_CONF))
                {
                    if (!this.UNIT.ADDINFO__V_DRY_RUN_TIMEOUT && bForcedTimeOut == false) //$ 2024.02.29 : NGTimeMode를 사용하는 경우 Timeout 첫 진입시 true가 되면 Bit를 Trigger하지 않게 함
                    {
                        this.UNIT.Variables[sender.Category.Name + ":" + CTag.O_B_TRIGGER_REPORT_CONF].AsBoolean = TrigStatus.TRIG_ON;
                        SOLLog(Level.Info, $"[STEP_7] {sender.Category.Name + ":" + CTag.O_B_TRIGGER_REPORT_CONF} : On", strLogCategory, strLogCat, SHOPID.FORM, txnID);
                    }

                    if (CVariableAction.TimeOut(sender, TrigStatus.TRIG_OFF, SCANINTERVAL, TIMEOUTSEC))
                    {
                        EIFLog(Level.Debug, $"[{sender.Category.Name}] Off TimeOut ({TIMEOUTSEC}sec)", strLogCategory, false, strLogCat, SHOPID.FORM);
                    }
                    else
                    {
                        SOLLog(Level.Info, $"[STEP_8] {sender.NameCategorized} : Off", strLogCategory, strLogCat, SHOPID.FORM, txnID);
                    }

                    if ((this.UNIT.ADDINFO__V_DRY_RUN && !this.UNIT.ADDINFO__V_DRY_RUN_DIRECT_INPUT) || !this.UNIT.ADDINFO__V_DRY_RUN)
                    {
                        foreach (CVariable vari in this.UNIT.Variables.Values.Where((p) => p.Category.Name == sender.Category.Name && p.AccessType == enumAccessType.Out))
                        {
                            if (vari.DataType == enumDataType.Short) vari.AsShort = ConfirmAck.CLEAR;
                            else if (vari.DataType == enumDataType.Integer) vari.AsInteger = ConfirmAck.CLEAR;
                            else if (vari.DataType == enumDataType.String) vari.AsString = String.Empty;
                            else if (vari.DataType == enumDataType.Boolean)
                            {
                                vari.AsBoolean = TrigStatus.TRIG_OFF;
                                SOLLog(Level.Info, $"[STEP_9] {sender.Category.Name + ":" + CTag.O_B_TRIGGER_REPORT_CONF} : Off", strLogCategory, strLogCat, SHOPID.FORM, txnID);
                            }
                        }
                    }
                }
            }
        }

        private void EQP_Trigger_OnVariableChanged<T>(CVariable sender, T value)
        {
            string strLogCat = strEqpID;

            EIFLog(Level.Verbose, $"{sender.NameCategorized} : {value}", strLogCategory, false, strLogCat, SHOPID.FORM);

            switch (sender.Category.Name)
            {
                default:
                    DispatchWordEvent(sender);
                    break;
            }
            GetVariableValueLog(sender);
        }

        private void HOST_Trigger_OnBooleanChanged(CVariable sender, bool value)
        {
            string strLogCat = strEqpID;

            try
            {
                EIFLog(Level.Verbose, $"{sender.NameCategorized} : {value}", strLogCategory, false, strLogCat, SHOPID.FORM);

                switch (value)
                {
                    case TrigStatus.TRIG_ON:

                        if (this.UNIT.Variables.ContainsKey($"{sender.Category.Name}:{CTag.I_B_HOST_TRIGGER_CONF}"))
                        {
                            if (CVariableAction.TimeOut(this.UNIT.Variables[$"{sender.Category.Name}:{CTag.I_B_HOST_TRIGGER_CONF}"], TrigStatus.TRIG_ON, SCANINTERVAL, TIMEOUTSEC))
                            {
                                EIFLog(Level.Debug, $"[{sender.Category.Name}] Off TimeOut ({TIMEOUTSEC}sec)", strLogCategory, false, strLogCat, SHOPID.FORM);
                                sender.AsBoolean = false;
                            }
                            else
                            {
                                sender.AsBoolean = false;
                            }
                        }
                        else
                        {
                            if (CVariableAction.TimeOut(sender, TrigStatus.TRIG_OFF, SCANINTERVAL, TIMEOUTSEC))
                            {
                                sender.AsBoolean = false;
                            }
                        }
                        break;

                    case TrigStatus.TRIG_OFF:

                        foreach (CVariable vari in this.UNIT.Variables.Values.Where((p) => p.Category.Name == sender.Category.Name && p.AccessType == enumAccessType.Out))
                        {
                            if (sender.NameCategorized.Equals(vari.NameCategorized)) continue;

                            if (vari.DataType == enumDataType.Short) vari.AsShort = ConfirmAck.CLEAR;
                            else if (vari.DataType == enumDataType.Integer) vari.AsInteger = ConfirmAck.CLEAR;
                            else if (vari.DataType == enumDataType.String) vari.AsString = string.Empty;
                            else if (vari.DataType == enumDataType.Boolean) vari.AsBoolean = TrigStatus.TRIG_OFF;
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strLogCat, SHOPID.FORM);
            }

        }

        private void GetVariableValueLog(CVariable sender, int iRet = 0)
        {
            StringBuilder sbInLog = new StringBuilder();
            StringBuilder sbOutLog = new StringBuilder();

            foreach (CVariable vari in this.UNIT.Variables.Values.Where(p => p.Category.Name == sender.Category.Name && (p.AccessType == enumAccessType.In || p.AccessType == enumAccessType.Out)))
            {
                if (vari.AccessType == enumAccessType.In)
                {
                    sbInLog.Append($"\"{vari.Name}:{vari.Value}\",");
                }
                else if (vari.AccessType == enumAccessType.Out)
                {
                    sbOutLog.Append($"\"{vari.Name}:{vari.Value}\",");
                }
            }

            EIFLog(Level.Verbose, $"[{sender.Category.Name}] [{(iRet == 0 ? "OK" : "NG")}] {sbInLog.ToString()}", strLogCategory, false, strEqpID, SHOPID.FORM);

            if (sbOutLog.Length != 0)
            {
                EIFLog(Level.Verbose, $"[{sender.Category.Name}] {sbOutLog.ToString()}", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
        }

        private void ADDINFO_O_B_RELOAD_VARIABLE_OnVariableOn(CVariable sender)
        {
            try
            {
                DateTime iTimeStart;
                TimeSpan time;
                string Value = string.Empty;
                foreach (CVariable var in this.UNIT.Variables.Values)
                {
                    if (var.AccessType == enumAccessType.In && ((CIOVariable)var).Driver != null)
                    {
                        iTimeStart = DateTime.Now;

                        Value = var.Value.ToString();

                        time = DateTime.Now - iTimeStart;

                        if (time.Milliseconds > 1)
                            EIFLog(Level.Debug, string.Format("[{0}] : {1} , ({2}), ({3})", var.NameCategorized, Value, time, var.ConnectionInfoString), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);

                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
        }

        //$ 2024.02.29 : NG/Timout 설정이 Off될 때 Dictionary 초기화(다시 테스트를 하거나 할 때)
        private void ADD_INFO__V_DRY_RUN_NGTIMEOUT_OnBooleanChanged(CVariable sender, bool value)
        {
            if (!value)
            {
                if (this.NakPassList != null) this.NakPassList.Clear();
                if (this.TimeOutPassList != null) this.TimeOutPassList.Clear();
            }
        }
        #endregion

        #region Communication Area
        #region 7.1.1.3 [C1-3] Communication State Change Report
        private void HostCommunicationCheck(CVariable value)
        {
            try
            {
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
            finally
            {
                this.UNIT.HOST_COMM_CHK__O_B_HOST_COMM_CHK = false;
            }
        }

        private void CommunicationStateChangeReport(CVariable sender, bool value)
        {
            if (value == TrigStatus.TRIG_ON)
            {
                EIFLog(Level.Verbose, $"[{sender.Name}] Communication State : ON", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
            else
            {
                EIFLog(Level.Verbose, $"[{sender.Name}] Communication State : OFF", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
        }
        #endregion

        #region 7.1.1.4 [C1-4] Date and Time Set Request
        private void DateAndTimeSetReqest(CVariable sender, bool value)
        {
            try
            {
                EIFLog(Level.Verbose, $"[{sender.Name}] Date and Time Set : {(value ? "ON" : "OFF")}", strLogCategory, false, strEqpID, SHOPID.FORM);

                switch (value)
                {
                    case TrigStatus.TRIG_ON:
                        if (CVariableAction.TimeOut(sender, TrigStatus.TRIG_OFF, SCANINTERVAL, COMMON_INTERVAL_SEC.SEC_3))
                        {
                            this.UNIT.DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ = false;
                            EIFLog(Level.Debug, $"[{sender.Name}] : TimeOut ({COMMON_INTERVAL_SEC.SEC_3}sec)", strLogCategory, false, strEqpID, SHOPID.FORM);
                        }
                        break;

                    case TrigStatus.TRIG_OFF:
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }
        #endregion

        #region 7.1.2.1 [C2-1] Equipment State Change Report

        private void SendEquipmentState(bool ConnectionState)
        {
            EIFLog(Level.Verbose, $"Driver Connection Changed [{(ConnectionState ? "ON" : "OFF")}]\r\n", strLogCategory, false, strEqpID, SHOPID.FORM);

            ushort iEqptStatsCD = this.UNIT.EQP_STAT_CHG_RPT__I_W_ALARM_ID;
            string strLotID = string.IsNullOrWhiteSpace(this.UNIT.EQP_STAT_CHG_RPT__I_W_CURRENT_LOT_ID) ? string.Empty : this.UNIT.EQP_STAT_CHG_RPT__I_W_CURRENT_LOT_ID.Trim().ToUpper();
            string EqptLotProgMode = this.UNIT.EQP_STAT_CHG_RPT__I_B_LOT_RUNNING ? EQPT_LOT_PROG_MODE.LampON : EQPT_LOT_PROG_MODE.LampOFF;

            ushort iEqptStat = 0;
            string strEqptSubState = string.Empty;
            if (ConnectionState)
            {
                iEqptStat = this.UNIT.EQP_STAT_CHG_RPT__I_W_EQP_STAT;
                strEqptSubState = this.UNIT.EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT.ToString();
            }
            if (this.BASE.Variables["ADDINFO:V_MAIN_EQP_ID"].AsString == strMachineID)
                RegEioState_V2(strEqpID, GetEIOState(iEqptStat), strEqptSubState, iEqptStatsCD.ToString(), bTestMode, strLotID, EqptLotProgMode);

            if (!string.IsNullOrWhiteSpace(strMachineID))
                RegEioState_V2(strMachineID, GetEIOState(iEqptStat), strEqptSubState, iEqptStatsCD.ToString(), bTestMode, strLotID, EqptLotProgMode);
        }

        private void EquipmentStateChangeReport(CVariable sender, ushort value)
        {
            try
            {
                EIFLog(Level.Verbose, $"[{sender.Category.Name}:{sender.Name}] : {value}", strLogCategory, false, strEqpID, SHOPID.FORM);

                ushort uEqptStat = this.UNIT.Variables[$"{sender.Category.Name}:I_W_EQP_STAT"].AsShort;

                if (sender.Name.Equals("I_W_EQP_SUBSTAT"))
                {
                    if (uEqptStat != EQPSTATUS.USER_STOP && uEqptStat != EQPSTATUS.WAIT) return;
                }
                else if (sender.Name.Equals("I_W_ALARM_ID"))
                {
                    if (uEqptStat != EQPSTATUS.TROUBLE || sender.AsShort == 0) return; // 2025.07.15 : int -> short 변경
                }

                ushort uEqptSubState = this.UNIT.Variables[$"{sender.Category.Name}:I_W_EQP_SUBSTAT"].AsShort;

                Wait(100); //$ 2025.07.07 : Main Userstop과 SubState가 동시에 바뀌는 경우를 처리하기 위해 SubState에 Delay를 줌

                if (this.UNIT.Variables[$"{sender.Category.Name}:I_W_EQP_SUBSTAT"].AsShort < 100) uEqptSubState = 0; // HDH 2023.07.28 : Substate 100 이하는 0으로 치환

                int iEqptTroblCD = 0;
                string strLotID = this.UNIT.EQP_STAT_CHG_RPT__I_W_CURRENT_LOT_ID.ToUpper().Trim();
                string EqptLotProgMode = this.UNIT.EQP_STAT_CHG_RPT__I_B_LOT_RUNNING ? EQPT_LOT_PROG_MODE.LampON : EQPT_LOT_PROG_MODE.LampOFF;


                switch (uEqptStat)
                {
                    case EQPSTATUS.WAIT:
                        break;

                    case EQPSTATUS.TROUBLE:
                        iEqptTroblCD = this.UNIT.Variables[$"{sender.Category.Name}:I_W_ALARM_ID"].AsShort;
                        break;

                    case EQPSTATUS.OFF:
                    case EQPSTATUS.RUN:
                        break;

                    case EQPSTATUS.USER_STOP:
                        break;
                }

                if (this.BASE.Variables["ADDINFO:V_MAIN_EQP_ID"].AsString == strMachineID)
                    RegEioState_V2(strEqpID, GetEIOState(uEqptStat), uEqptSubState.ToString(), iEqptTroblCD.ToString(), bTestMode, strLotID, EqptLotProgMode);

                if (!string.IsNullOrWhiteSpace(strMachineID))
                    RegEioState_V2(strMachineID, GetEIOState(uEqptStat), uEqptSubState.ToString(), iEqptTroblCD.ToString(), bTestMode, strLotID, EqptLotProgMode);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }
        #endregion

        #region 7.1.2.3 [C2-3] Host Alarm Message Send
        public void SendHostAlarm(Exception BizRuleErr, string strLangID, ushort uDisplayType)
        {
            Task.Factory.StartNew(() =>
            {
                strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;    //$ 2023.02.06 : 활성화는 영문만 사용
                SendHostAlarmMsg(BizRuleErr, strLangID, uDisplayType);
            });
        }
        #endregion

        #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report

        private void OperationModeChanged(CVariable sender, bool value)
        {
            EIFLog(Level.Verbose, $"[{sender.Name}] Operation Mode : {(value ? "ON" : "OFF")}\r\n", strLogCategory, false, strEqpID, SHOPID.FORM);
        }

        private void ITBypassChanged(CVariable sender, bool value)
        {
            EIFLog(Level.Verbose, $"[{sender.Name}] IT Bypass Mode : {(value ? "ON" : "OFF")}\r\n", strLogCategory, false, strEqpID, SHOPID.FORM);

            ITBypassModeOff();
        }

        private void HMILangTypeChanged(CVariable sender, ushort value)
        {
            try
            {
                GuiLanguageTypeChanged(value);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        #endregion

        #region 7.1.2.6 [C2-6] Remote Command Send

        private void RemoteCommandSend(CVariable sender, bool value)
        {
            EIFLog(Level.Verbose, $"[{sender.Name}] Remote Command Send : {(value ? "ON" : "OFF")}", strLogCategory, false, strEqpID, SHOPID.FORM);

            try
            {
                switch (value)
                {
                    case TrigStatus.TRIG_ON:
                        string sCommandCode = GetRemoteCommandCode(this.UNIT.REMOTE_COMM_SND__O_W_REMOTE_COMMAND_CODE);
                        EIFLog(Level.Verbose, $"[{sender.Name}] Remote Command Code : {sCommandCode}", strLogCategory, false, strEqpID, SHOPID.FORM);

                        if (CVariableAction.TimeOut(sender, TrigStatus.TRIG_OFF, SCANINTERVAL, TIMEOUTSEC))
                        {
                            this.UNIT.REMOTE_COMM_SND__O_B_REMOTE_COMMAND_SEND = false;
                            EIFLog(Level.Debug, $"[{sender.Name}] TimeOut ({TIMEOUTSEC}sec)", strLogCategory, false, strEqpID, SHOPID.FORM);
                        }
                        break;

                    case TrigStatus.TRIG_OFF:
                        this.UNIT.REMOTE_COMM_SND__O_W_REMOTE_COMMAND_CODE = 0;
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        private void RemoteCommandConfirm(CVariable sender, bool value)
        {
            EIFLog(Level.Verbose, $"[{sender.Name}] Remote Command Confirm : {(value ? "ON" : "OFF")}", strLogCategory, false, strEqpID, SHOPID.FORM);

            try
            {
                if (this.UNIT.REMOTE_COMM_SND__I_B_REMOTE_COMMAND_CONF)
                {
                    string strCommandCode = GetRemoteCommandCode(this.UNIT.REMOTE_COMM_SND__O_W_REMOTE_COMMAND_CODE);

                    string sCommandACK = string.Empty;
                    switch (this.UNIT.REMOTE_COMM_SND__I_W_REMOTE_COMMAND_CONF_ACK)
                    {
                        case 10:
                            sCommandACK = "OK";
                            break;
                        case 11:
                            sCommandACK = "NG";
                            break;
                        default:
                            break;
                    }

                    this.UNIT.REMOTE_COMM_SND__O_B_REMOTE_COMMAND_SEND = false;

                    SendHostAlarmMsg($"[Host Command Result {sCommandACK}] {strCommandCode}", HOST_ALM_TYPE.COMM_TYPE, strLangID);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        private void ITBypassModeOff()
        {
            if (!this.UNIT.ADDINFO__V_USE_IT_BYPASS && this.UNIT.EQP_OP_MODE_CHG_RPT__I_B_IT_BYPASS)
            {
                SetRemoteCommand(REMOTE_CMD.IT_BY_PASS_RELEASE, strEqpID);
                SendHostAlarmMsg("[Remote] IT Bypass Mode is released !", HOST_ALM_TYPE.COMM_TYPE, strLangID);
            }
        }

        private string GetRemoteCommandCode(int iCode)
        {
            string strCommandCode = string.Empty;

            switch (iCode)
            {
                case 1:
                    strCommandCode = "RMS Control State Change to Online Remote";
                    break;
                case 12:
                    strCommandCode = "IT Bypass Mode Released";
                    break;
                case 13:
                    strCommandCode = "Lot Control Mode Change to Remote";
                    break;
                case 14:
                    strCommandCode = "Lot Control Mode Change to Local";
                    break;
                case 21:
                    strCommandCode = "Processing Pause";
                    break;
                case 31:
                    strCommandCode = "Lot Start";
                    break;
                case 32:
                    strCommandCode = "Lot Change";
                    break;
                case 33:
                    strCommandCode = "Lot End";
                    break;
                default:
                    break;
            }

            return strCommandCode;
        }

        private void SetRemoteCommand(ushort uCode, string strEqpID)
        {
            lock (objRemoteCommandLock)
            {
                this.UNIT.REMOTE_COMM_SND__O_W_REMOTE_COMMAND_CODE = uCode;
                this.UNIT.REMOTE_COMM_SND__O_B_REMOTE_COMMAND_SEND = TrigStatus.TRIG_ON;

            }
        }

        #endregion

        #region 7.1.2.8 [C2-8] Alarm Set Report //$ 2023.02.02 : Alarm 다중 보고 대응을 위한 추가
        private void AlarmSetRequest(CVariable sender, bool value)
        {
            try
            {
                bool bLogging = this.UNIT.BASICINFO__V_IS_ALMLOG_USE; //$ 2023.05.19 : Log 사용 여부는 Virtual 변수로 판단

                switch (value)
                {
                    case TrigStatus.TRIG_ON:
                        {
                            ushort uAlarmID = this.UNIT.ALARM_RPT__I_W_ALARM_SET_ID;
                            ushort uEioState = this.UNIT.EQP_STAT_CHG_RPT__I_W_EQP_STAT;

                            if (bLogging) EIFLog(Level.Verbose, $"[{sender.Name}] Alarm Set Request : {(value ? "ON" : "OFF")} - AlarmID : {uAlarmID}", strLogCategory, false, strEqpID, SHOPID.FORM);

                            Wait(100); //$ 2023.06.28 : 설비 Trouble 보고와 Alarm Set이 동시에 보고 될 경우 Dup 발생하여 Set은 약간의 시간 Delay를 줌
                            RegEqptSetAlarm(strEqpID, "", uEioState.ToString(), uAlarmID.ToString(), bTestMode, bLogging);

                            this.UNIT.ALARM_RPT__O_B_ALARM_SET_CONF = true;
                        }
                        break;

                    case TrigStatus.TRIG_OFF:
                        {
                            if (bLogging) EIFLog(Level.Verbose, $"[{sender.Name}] Alarm Set Request : {(value ? "ON" : "OFF")}", strLogCategory, false, strEqpID, SHOPID.FORM);

                            this.UNIT.ALARM_RPT__O_B_ALARM_SET_CONF = false;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }
        #endregion

        #region 7.1.2.9 [C2-9] Alarm Reset Report //$ 2023.02.02 : Alarm 다중 보고 대응을 위한 추가
        private void AlarmResetRequest(CVariable sender, bool value)
        {
            try
            {
                bool bLogging = this.UNIT.BASICINFO__V_IS_ALMLOG_USE; //$ 2023.05.19 : Log 사용 여부는 Virtual 변수로 판단

                switch (value)
                {
                    case TrigStatus.TRIG_ON:
                        {
                            ushort uAlarmID = this.UNIT.ALARM_RPT__I_W_ALARM_RESET_ID;
                            ushort uEioState = this.UNIT.EQP_STAT_CHG_RPT__I_W_EQP_STAT;

                            if (bLogging) EIFLog(Level.Verbose, $"[{sender.Name}] Alarm Reset Request : {(value ? "ON" : "OFF")} - AlarmID : {uAlarmID}", strLogCategory, false, strEqpID, SHOPID.FORM);

                            RegEqptResetAlarm(strEqpID, "", uEioState.ToString(), uAlarmID.ToString(), bTestMode, bLogging);

                            this.UNIT.ALARM_RPT__O_B_ALARM_RESET_CONF = true;
                        }
                        break;

                    case TrigStatus.TRIG_OFF:
                        {
                            if (bLogging) EIFLog(Level.Verbose, $"[{sender.Name}] Alarm Reset Request : {(value ? "ON" : "OFF")}", strLogCategory, false, strEqpID, SHOPID.FORM);

                            this.UNIT.ALARM_RPT__O_B_ALARM_RESET_CONF = false;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }
        #endregion

        #region 7.1.2.10 [C2-10] Smoke Detect Report
        private void SmokeDetectReport(CVariable sender, bool value)
        {
            try
            {
                switch (value)
                {
                    case TrigStatus.TRIG_ON:
                        {
                            ushort uSmokeState = this.UNIT.SMOKE_RPT__I_W_EQP_SMOKE_STATUS;

                            EIFLog(Level.Verbose, $"[{sender.Name}] Smoke Detect Report : {(value ? "ON" : "OFF")} - Smoke Status : {uSmokeState}", strLogCategory, false, strEqpID, SHOPID.FORM);

                            RegSmokeDetectREQ(strEqpID, uSmokeState.ToString(), bTestMode);

                            this.UNIT.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF = true;
                        }
                        break;

                    case TrigStatus.TRIG_OFF:
                        {
                            EIFLog(Level.Verbose, $"[{sender.Name}] Smoke Detect Report : {(value ? "ON" : "OFF")}", strLogCategory, false, strEqpID, SHOPID.FORM);

                            this.UNIT.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF = false;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }
        #endregion
        #endregion

        #region Processing Area
        protected virtual int DispatchEvent(CVariable sender, string txnID)
        {
            foreach (CReportInfo info in this.UNIT.ReportList.Where(a => -1 < a.getReportType(sender.Category.Name)))
            {
                switch (info._Type)
                {
                    case (int)REPORT_TYPE.PALLET_ID_RPT:
                        return PALLET_ID_RPT(sender, txnID);

                    case (int)REPORT_TYPE.TRAY_ID_RPT:
                        return TRAY_ID_RPT(sender, txnID);

                    case (int)REPORT_TYPE.PALLET_JOB_START_RPT:
                        return PALLET_JOB_START_RPT(sender, txnID);

                    case (int)REPORT_TYPE.TRAY_JOB_START_RPT:
                        return TRAY_JOB_START_RPT(sender, txnID);

                    case (int)REPORT_TYPE.PALLET_OUT_RPT:
                        return PALLET_OUT_RPT(sender);

                    case (int)REPORT_TYPE.PALLET_JOB_END_RPT:
                        return PALLET_JOB_END_RPT(sender, txnID);

                    case (int)REPORT_TYPE.TRAY_END_PACKING:
                        return TRAY_END_PACKING(sender, txnID);

                    case (int)REPORT_TYPE.PALLET_CHECK_CONFIRM:
                        return PALLET_CHECK_CONFIRM(sender, txnID);

                    case (int)REPORT_TYPE.APD_RPT:
                        return APD_RPT(sender, txnID);

                    case (int)REPORT_TYPE.CELL_ID_CONF_REQ:
                        return CELL_ID_CONF_REQ(sender, txnID);

                    case (int)REPORT_TYPE.CELL_OUT_NG:
                        return CELL_OUT_NG(sender, txnID);

                    case (int)REPORT_TYPE.CELL_INFO_REQ:
                        return CELL_INFO_REQ(sender);

                    case (int)REPORT_TYPE.MODEL_ID_CHG:
                        return MODEL_ID_CHG(sender, txnID);

                    case (int)REPORT_TYPE.LINE_ID_CHG:
                        return LINE_ID_CHG(sender, txnID);

                    case (int)REPORT_TYPE.FIRST_TRAY_USE_CHG:
                        return FIRST_TRAY_USE_CHG(sender);

                    case (int)REPORT_TYPE.PALET_INFO_REQ:
                        return PALET_INFO_REQ(sender, txnID);

                    default:
                        break;
                }
            }
            return -1;
        }

        protected virtual void DispatchWordEvent(CVariable sender)
        {
            foreach (CReportInfo info in this.UNIT.ReportList.Where(a => -1 < a.getReportType(sender.Category.Name)))
            {

                switch (info._Type)
                {
                    //// JH 2025.06.13 재료 교체 알람 추가 [UC2 반영] + 재료 교체 사용안함으로 해당영역만 주석표시
                    //case (int)REPORT_TYPE.MTRL_STAT_CHG_RPT:
                    //    MTRL_STAT_CHG_RPT(sender);
                    //    break;

                    case (int)REPORT_TYPE.LR_PORT_STAT_CHG:
                    case (int)REPORT_TYPE.UR_PORT_STAT_CHG:
                        if (sender.Name.Equals($"{CTag.I_W_TRIGGER_}STAT"))
                            PORT_STAT_CHG(sender, (int)info._Type);
                        else if (sender.Name.Equals($"{CTag.I_W_TRIGGER_}CARRIER_ID"))
                            PORT_STAT_CHG(sender, (int)info._Type);
                        else if (sender.Name.Equals($"{CTag.I_W_TRIGGER_}OP_MODE"))
                            PORT_OP_MODE_CHG(sender);
                        break;
                    default:
                        break;
                }
            }
        }

        #region 7.2.1.1 [G2-1] Material Monitoring Data Report
        // PBK 2024.03.14 재료 교체 알람 추가
        private void MTRL_STAT_CHG_RPT(CVariable sender)
        {
            int iRet = -1;
            Exception BizRuleErr = null;
            string strEqptID = Variables["BASICINFO:V_EQP_MACHINE_ID_01"].AsString;

            try
            {
                //__MTRL_STAT_CHG_RPT__I_W_TRIGGER_MTRL_STAT_CHG_EVENT_CODE.ToString();
                string strEventCode = this.UNIT.__G1_1_MTRL_STAT_CHG__I_W_TRIGGER_MTRL_STAT_CHG_EVENT_CODE.ToString();

                //string strLotId = TAB2_PROC_RST_DATA_RPT__I_W_CURRENT_LOT_ID.Trim().ToUpper();
                string strLotId = string.Empty;

                if (string.IsNullOrWhiteSpace(strEventCode))
                {
                    return;
                }

                EIFLog(Level.Verbose, string.Format("{0} : Material State Change Report", sender.Name), strLogCategory, false, strEqpID, SHOPID.ASSY);

                iRet = BrEqpRegEqptWorkEventEMSToMq(false, strEqptID, strLotId, strEventCode, out BizRuleErr);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.ASSY);
            }
        }
        #endregion

        #region 7.2.2.1 [G2-1] Carrier ID Report
        private int PALLET_ID_RPT(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                string strModelID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_MODEL_ID.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;
                string strLineID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_LINE_ID.Trim();
                string strInputPalletID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_ID"].AsString.Trim().ToUpper();
                string strPstnID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_ID"].ConnectionInfo["PSTN_ID", string.Empty].ToString();
                bool bRFIDPassMode = this.UNIT.Variables[$"{sender.Category.Name}:I_B_PALLET_RFID_PASS_MODE"].AsBoolean;

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_PALLET_ID"} : {strInputPalletID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start - PalletID : [{strInputPalletID}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                string strScanRst = string.Empty;

                if (string.IsNullOrWhiteSpace(strInputPalletID) || Regex.IsMatch(strInputPalletID.ToUpper(), @"NO\s+READ|NOREAD"))
                {
                    SendHostAlarmMsg($"[{sender.Category.Name}] Carrier ID is Empty", HOST_ALM_TYPE.COMM_TYPE);

                    strScanRst = "NG";
                }
                else if (string.IsNullOrEmpty(strModelID)) //$ 2022.05.22 : ModelID가 없을 경우 잘못된 ProductID가 만들어져 셀 적재하고 나는 문제를 사전에 예방
                {
                    SendHostAlarmMsg($"[{sender.Category.Name}] Model ID is Empty", HOST_ALM_TYPE.COMM_TYPE);

                    strScanRst = "NG";
                }
                else
                {
                    #region Declare OutData
                    string strPalletID = string.Empty;
                    #endregion

                    iRet = bizBrFormGetNewPackingPalletID(false, strEqpID, txnID, strLineID, strSubModelID, strInputPalletID, out strPalletID, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                    switch (iRet)
                    {
                        case 0:
                            this.UNIT.Variables[$"{sender.Category.Name}:O_W_PALLET_LOT_ID"].AsString = strPalletID.ToUpper();
                            break;

                        default:
                            SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                            break;
                    }

                    strScanRst = "OK";
                }

                SendRfidReadingResult(sender, strPstnID, strScanRst, INOUT_TYPE.IN, strInputPalletID);

                EIFLog(Level.Verbose, $"<======= {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] End - iRet : [{iRet}], Rst : [{strScanRst}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                //$ 2023.02.22 : LC에서 Pallet ID를 Unknown으로 주기때문에 Pallet ID 보고 시점으로 위치 이동
                string strPortID = this.UNIT.Variables["T1_1_PORT_STAT_CHG_01:I_W_TRIGGER_STAT"].ConnectionInfo["PSTN_ID", string.Empty].ToString();
                brMhsEifRegEqptLoadedPallet(false, strEqpID, strPortID, strInputPalletID, out BizRuleErr);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }

        private int TRAY_ID_RPT(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                string strModelID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_MODEL_ID.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;
                string strLineID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_LINE_ID.Trim();
                string strInputTrayID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_TRAY_ID"].AsString.Trim().ToUpper();

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_TRAY_ID"} : {strInputTrayID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start - TrayID : [{strInputTrayID}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                if (string.IsNullOrWhiteSpace(strInputTrayID) || Regex.IsMatch(strInputTrayID.ToUpper(), @"NO\s+READ|NOREAD"))
                {
                    SendHostAlarmMsg($"[{sender.Category.Name}] Carrier ID is Empty", HOST_ALM_TYPE.COMM_TYPE);
                }
                else
                {
                    string strTrayID = string.Empty;

                    iRet = bizBrFormGetNewPackingBoxID(false, strEqpID, txnID, strLineID, strSubModelID, strInputTrayID, out strTrayID, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                    switch (iRet)
                    {
                        case 0:
                            this.UNIT.Variables[$"{sender.Category.Name}:O_W_HOST_TRAY_ID"].AsString = strTrayID.ToUpper();
                            break;

                        default:
                            SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                            break;
                    }
                }

                EIFLog(Level.Verbose, $"<======= {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] End - iRet : [{iRet}]", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.2.2.2 [G2-2] Carrier Input Report
        private int PALLET_JOB_START_RPT(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                string strModelID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_MODEL_ID.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;
                string strLineID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_LINE_ID.Trim();
                string strPalletLotID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_LOT_ID"].AsString.Trim().ToUpper();

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_PALLET_LOT_ID"} : {strPalletLotID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start - PalletLotID : [{strPalletLotID}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                string strTrayID = string.Empty;

                iRet = bizBrFormRegStartPackingPallet(false, strEqpID, txnID, strLineID, strSubModelID, strPalletLotID, out strTrayID, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                switch (iRet)
                {
                    case 0:
                        this.UNIT.Variables[$"{sender.Category.Name}:O_W_HOST_TRAY_ID"].AsString = strTrayID.ToUpper();
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }

                EIFLog(Level.Verbose, $"<======= {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] End - iRet : [{iRet}]", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }

        private int TRAY_JOB_START_RPT(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                string strModelID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_MODEL_ID.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;
                string strLineID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_LINE_ID.Trim();

                string strPalletLotID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_LOT_ID"].AsString.Trim().ToUpper();
                string strTrayID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_TRAY_ID"].AsString.Trim().ToUpper();

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_PALLET_LOT_ID"} : {strPalletLotID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);
                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_TRAY_ID"} : {strTrayID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start - PalletLotID : [{strPalletLotID}], TrayID : [{strTrayID}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                iRet = bizBrFormRegStartPackingBox(false, strEqpID, txnID, strLineID, strSubModelID, strPalletLotID, strTrayID, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                switch (iRet)
                {
                    case 0:
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }

                EIFLog(Level.Verbose, $"<======= {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] End - iRet : [{iRet}]", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.2.2.3 [G2-3] Carrier Output Report
        private int PALLET_OUT_RPT(CVariable sender)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start", strLogCategory, false, strEqpID, SHOPID.FORM);

            try
            {
                iRet = 0;

                switch (iRet)
                {
                    case 0:
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }

                EIFLog(Level.Verbose, $"<======= {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] End - iRet : [{iRet}]", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.2.2.4 [G2-4] Carrier State Change Report
        private int TRAY_END_PACKING(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                Wait(200);

                ushort uTraySeqNum = this.UNIT.Variables[$"{sender.Category.Name}:I_W_TRAY_SEQ_NO"].AsShort;

                if (this.UNIT.Variables[$"{sender.Category.Name}:I_B_TRAY_EMPTY_STAT"].AsBoolean)
                {
                    EIFLog(Level.Debug, $"[{sender.Category.Name}] [#{uTraySeqNum:D2}] Tray is Empty", strLogCategory, false, strEqpID, SHOPID.FORM);

                    //SendHostAlarmMsg($"[#{uTraySeqNum:D2}] Tray is Empty", HOST_ALM_TYPE.COMM_TYPE);  //$ 2022.05.20 : Trouble 내고 Ack 0으로 바꿔서 진행이 안됨, 공 Tray도 Host 보고 하는 것으로 수정
                    //return 0; // Tray 강제배출 모드임. 상시 ACK = ON으로 처리함.
                }

                #region [ Cell 중복 확인 ]
                string strCellID = string.Empty;

                Dictionary<string, int> dicItem = new Dictionary<string, int>();

                for (int i = 1; i <= this.UNIT.Variables[$"BASICINFO:V_PACKING_CELL_COUNT"].AsInteger; i++)      // V_PACKING_CELL_COUNT 1Tray 당 포장되는 Cell 카운트
                {
                    strCellID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_CELL_ID_{i:D2}"].AsString.Trim();

                    if (!string.IsNullOrWhiteSpace(strCellID))
                    {
                        if (!dicItem.ContainsKey(strCellID))
                        {
                            dicItem.Add(strCellID, i);
                        }
                        else
                        {
                            SendHostAlarmMsg($"[{sender.Name}] Duplicate Cell ID : {strCellID}", HOST_ALM_TYPE.COMM_TYPE);
                            return -1;
                        }
                    }
                }
                #endregion

                string strModelID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_MODEL_ID.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;
                string strLineID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_LINE_ID.Trim();

                string strPalletLotID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_LOT_ID"].AsString.Trim().ToUpper();
                string strTrayID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_TRAY_ID"].AsString.Trim().ToUpper();
                ushort uTrayCellQty = this.UNIT.Variables[$"{sender.Category.Name}:I_W_TRAY_CELL_CNT"].AsShort;
                bool bTrayEmpty = this.UNIT.Variables[$"{sender.Category.Name}:I_B_TRAY_EMPTY_STAT"].AsBoolean;

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_TRAY_ID"} : {strTrayID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);
                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_PALLET_LOT_ID"} : {strPalletLotID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start - SeqNo : [{uTraySeqNum}], TrayID : [{strTrayID}], CellQty : [{uTrayCellQty}] >>>>>", strLogCategory, false, strEqpID, SHOPID.FORM);

                #region IN_PALLET
                List<CInPallet> lstPallet = new List<CInPallet>();
                CInPallet cInPallet = new CInPallet();
                cInPallet.PALLETID = strPalletLotID;
                cInPallet.BOXID = strTrayID;
                cInPallet.PACKING_QTY = uTrayCellQty;
                cInPallet.EMPTY_TRAY_FLAG = bTrayEmpty ? "Y" : "N";
                lstPallet.Add(cInPallet);
                #endregion

                #region IN_BOX
                List<CInBox> lstBox = new List<CInBox>();

                foreach (KeyValuePair<string, int> Cell in dicItem)
                {
                    CInBox cInBox = new CInBox();
                    cInBox.PSTN_NO = Cell.Value;
                    cInBox.SUBLOTID = Cell.Key;
                    lstBox.Add(cInBox);
                }
                #endregion

                iRet = bizBrFormRegEndPackingBox(false, strEqpID, txnID, strLineID, strSubModelID, lstPallet, lstBox, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                switch (iRet)
                {
                    case 0:
                        //Variables[$"BASICINFO:V_PACKING_TRAY_COUNT"].AsInteger = 30;  //$ 2022.05.20 : 주석 처리, 이전에 DB에서 가지고 왔던 값인데.. 활성화에는 없으므로 주석 처리
                        //Variables[$"BASICINFO:V_PACKING_CELL_COUNT"].AsInteger = 20;  //$ 2022.05.20 : 주석 처리, 이전에 DB에서 가지고 왔던 값인데.. 활성화에는 없으므로 주석 처리
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }

                EIFLog(Level.Verbose, $"<======= {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] End - iRet : [{iRet}]", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }

        private int PALLET_CHECK_CONFIRM(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                EIFLog(Level.Verbose, $"[{sender.Category.Name}] Carrier State Change Report 02 : On", strLogCategory, false, strEqpID, SHOPID.FORM);

                string strPalletID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_ID"].AsString.Trim().ToUpper();

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_PALLET_ID"} : {strPalletID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start - PalletID : [{strPalletID}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                iRet = bizBrFormChkPalletOut(bTestMode, strEqpID, txnID, strPalletID, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                switch (iRet)
                {
                    case 0:
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }

                EIFLog(Level.Verbose, $"<======= {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] End - iRet : [{iRet}]", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.2.2.6 [G2-6] Carrier Job End Report
        private int PALLET_JOB_END_RPT(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                string strModelID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_MODEL_ID.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;
                string strLineID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_LINE_ID.Trim();

                string strPalletID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_ID"].AsString.Trim().ToUpper();
                string strPalletLotID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_LOT_ID"].AsString.Trim().ToUpper();
                ushort uCellQty = this.UNIT.Variables[$"{sender.Category.Name}:I_W_TOTAL_CELL_CNT"].AsShort;

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_PALLET_ID"} : {strPalletID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);
                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_PALLET_LOT_ID"} : {strPalletLotID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start - PalletID : [{strPalletID}], PalletLotID : [{strPalletLotID}], TotalQty : {uCellQty}", strLogCategory, false, strEqpID, SHOPID.FORM);

                #region IN_PALLET
                List<CInPallet> lstPallet = new List<CInPallet>();
                CInPallet cInPallet = new CInPallet();
                cInPallet.PALLETID = strPalletLotID;
                cInPallet.PACKING_QTY = uCellQty;
                lstPallet.Add(cInPallet);
                #endregion

                #region IN_BOX
                List<CInBox> lstBox = new List<CInBox>();
                CInBox cInBox = new CInBox();
                #endregion

                iRet = bizBrFormRegEndPackingPallet(false, strEqpID, txnID, strLineID, strSubModelID, lstPallet, lstBox, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                EIFLog(Level.Verbose, $"=======> BR_FORM_REG_END_PACKING_PALLET Call - iRet : [{iRet}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                if (iRet == 0)
                {
                    #region OutData
                    List<COutPallet> lstOutPallet = null;
                    #endregion

                    //$ 2022.05.23 : Pallet 확정 위치로 이동 후 ID Read인데.. 실제 BCR이 없으므로 Pallet Job End Method 안쪽으로 이동
                    iRet = bizBrPrdGetPalletInfoByCstId(false, strEqpID, txnID, strLineID, strSubModelID, strPalletID, out lstOutPallet, out BizRuleErr);  //$ 2022.05.20 : PalletID를 인자로 줘야 함. //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                    EIFLog(Level.Verbose, $"=======> BR_PRD_GET_PALLET_INFO_BY_CSTID Call - iRet : [{iRet}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                    if (iRet == 0)
                    {
                        if (lstOutPallet[0].EQPT_END_FLAG.Equals(YESNO.Yes))
                        {
                            //$ 2022.05.23 : Pallet 확정 보고 인데.. 실제 BCR이 없으므로 Pallet Job End Method 안쪽으로 이동
                            iRet = bizBrFormChkPalletOut(bTestMode, strEqpID, txnID, strPalletID, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                            EIFLog(Level.Verbose, $"=======> BR_FORM_CHK_PALLET_OUT Call - iRet : [{iRet}]", strLogCategory, false, strEqpID, SHOPID.FORM);
                        }
                        else
                        {
                            SendHostAlarmMsg($"[Error] Pallet Not Complete", HOST_ALM_TYPE.COMM_TYPE);
                            return -1;
                        }
                    }
                }

                switch (iRet)
                {
                    case 0:
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.2.3.5 [G3-5] Actual Processing Data Report
        private int APD_RPT(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = 0;

            try
            {
                string strLotID = string.Empty;
                int iTempVal = 0;
                ushort usTempVal = 0;
                double dCalVal = 0;

                string strClctItemName = string.Empty;
                int iAPDFpoint = 0;
                string strVarName = string.Empty;
                int iCellPosition = 0;

                Dictionary<string, List<string>> dicClctItem = new Dictionary<string, List<string>>();
                List<string> lstClctItemName = new List<string>();
                Dictionary<string, string> dicJudge = new Dictionary<string, string>();

                string strCellID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_CELL_ID"].AsString.Trim();

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_CELL_ID"} : {strCellID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start - CellID : [{strCellID}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                string sNo = sender.Category.Name.Substring(sender.Category.Name.Length - 2);

                for (int i = 1; i <= this.UNIT.ADDINFO__V_CELL_CLCT_DATA_CNT; i++)
                {
                    if (!string.IsNullOrWhiteSpace(_strlstApdData[i]))
                    {
                        List<string> lstClctItemValue = new List<string>();
                        strClctItemName = _strlstApdData[i];
                        iAPDFpoint = _ilstApdDataFPoint[i];

                        strVarName = $"{sender.Category.Name}:APD_CELL_DATA_{(i):D3}";
                        iCellPosition = i;

                        switch (this.UNIT.Variables[strVarName].DataType)
                        {
                            case enumDataType.String:
                                lstClctItemValue.Add(this.UNIT.Variables[strVarName].AsString.Trim());
                                lstClctItemValue.Add(iCellPosition.ToString());
                                lstClctItemValue.Add(strCellID);

                                // 재작업시 판정값 확인위해 추가 보고 - APD 항목변경시 반드시 확인 필요
                                if (i > 65) dicJudge.Add(strClctItemName, this.UNIT.Variables[strVarName].AsString.Trim());
                                break;

                            case enumDataType.Integer:
                                iTempVal = this.UNIT.Variables[strVarName].AsInteger;
                                dCalVal = iTempVal / Math.Pow(10, iAPDFpoint);

                                lstClctItemValue.Add(string.IsNullOrWhiteSpace(dCalVal.ToString()) ? string.Empty : dCalVal.ToString());
                                lstClctItemValue.Add(iCellPosition.ToString());
                                lstClctItemValue.Add(strCellID);
                                break;

                            case enumDataType.Short:
                                usTempVal = this.UNIT.Variables[strVarName].AsShort;
                                dCalVal = iTempVal / Math.Pow(10, iAPDFpoint);

                                lstClctItemValue.Add(string.IsNullOrWhiteSpace(dCalVal.ToString()) ? string.Empty : dCalVal.ToString());
                                lstClctItemValue.Add(iCellPosition.ToString());
                                lstClctItemValue.Add(strCellID);
                                break;

                            case enumDataType.Boolean:
                                lstClctItemValue.Add(this.UNIT.Variables[strVarName].AsBoolean ? "OK" : "NG");
                                lstClctItemValue.Add(iCellPosition.ToString());
                                lstClctItemValue.Add(strCellID);
                                break;

                            case enumDataType.ShortList:
                                List<int> ilCellPrintTime = new List<int>();

                                foreach (ushort us in this.UNIT.Variables[strVarName].AsShortList)
                                {
                                    ilCellPrintTime.Add(us);
                                }

                                DateTime dt = new DateTime(ilCellPrintTime[0], ilCellPrintTime[1], ilCellPrintTime[2], ilCellPrintTime[3], ilCellPrintTime[4], ilCellPrintTime[5]);

                                lstClctItemValue.Add(dt.ToString());
                                lstClctItemValue.Add(iCellPosition.ToString());
                                lstClctItemValue.Add(strCellID);
                                break;
                        }

                        lstClctItemName.Add(strClctItemName);
                        dicClctItem.Add(strClctItemName, lstClctItemValue);
                    }
                }

                if (string.IsNullOrWhiteSpace(strCellID) == false)
                {
                    //$ 2022.10.20 : 비동기 Thread로 돌리게 될 경우 CellID 보고보다 이후에 APD Biz 호출이 될 경우가 있다. 이걸 왜 쓴거지 모르것음. 주석 처리
                    //$ 2025.08.28 : Packer의 Vision APD 보고는 MES와 무관하며 대용량 데이터 처리로 인한 Tact 지연을 막고자 비동기 Thread로 병렬 처리 함
                    System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        RegEqptDataClct_APD(strEqpID, txnID, string.Empty, strLotID, strCellID, 1, sender.Name, lstClctItemName, dicClctItem, string.Empty, this.UNIT.Variables["BASICINFO:V_APD_LOGGING"].AsBoolean); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                    });

                    bizBrRegPackingVisionJudb(false, strEqpID, txnID, strCellID, dicJudge, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                    EIFLog(Level.Verbose, $"<======= APD_RPT End - CellID : [{strCellID}]", strLogCategory, false, strEqpID, SHOPID.FORM);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.2.4.1 [G4-1] Cell ID Confirm Request
        private int CELL_ID_CONF_REQ(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                string strModelID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_MODEL_ID.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;
                string strLineID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_LINE_ID.Trim();

                string strCellID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_CELL_ID"].AsString.Trim();

                string strVisionNG001 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_001"].AsString.Trim();
                string strVisionNG002 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_002"].AsString.Trim();
                string strVisionNG003 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_003"].AsString.Trim();
                string strVisionNG004 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_004"].AsString.Trim();
                string strVisionNG005 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_005"].AsString.Trim();
                string strVisionNG006 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_006"].AsString.Trim();
                string strVisionNG007 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_007"].AsString.Trim();
                string strVisionNG008 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_008"].AsString.Trim();
                string strVisionNG009 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_009"].AsString.Trim();
                string strVisionNG010 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_010"].AsString.Trim();
                string strVisionNG011 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_011"].AsString.Trim();
                string strVisionNG012 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_012"].AsString.Trim();
                string strVisionNG013 = this.UNIT.Variables[$"{sender.Category.Name}:I_W_VISION_JUDG_013"].AsString.Trim();

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_CELL_ID"} : {strCellID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start - CellID : [{strCellID}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                string OutLotID = string.Empty;
                string OuProdID = string.Empty;

                int NGCode = 0;

                if (this.UNIT.ADDINFO__V_WAIT_TIME > 0)
                {
                    Wait(this.UNIT.ADDINFO__V_WAIT_TIME);
                }

                iRet = bizBrFormChkVisionJudgCell(false, strEqpID, txnID, strLineID, strSubModelID, strCellID, strVisionNG001, strVisionNG002, strVisionNG003, strVisionNG004, strVisionNG005, strVisionNG006, strVisionNG007, strVisionNG008, strVisionNG009, strVisionNG010, strVisionNG011, strVisionNG012, strVisionNG013, out NGCode, out BizRuleErr);  //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                if (iRet == 0)
                {
                    iRet = bizBrFormChkPackingCell(false, strEqpID, txnID, strLineID, strSubModelID, strCellID, out OutLotID, out OuProdID, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                }

                switch (iRet)
                {
                    case 0:
                        this.UNIT.Variables[$"{sender.Category.Name}:O_W_NG_TYPE"].AsInteger = NGCode;
                        this.UNIT.Variables[$"{sender.Category.Name}:O_W_PKG_LOT_ID"].AsString = OutLotID.ToUpper();
                        this.UNIT.Variables[$"{sender.Category.Name}:O_W_PRODUCT_ID"].AsString = OuProdID;
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }

                EIFLog(Level.Verbose, $"<======= {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] End - LotID : [{OutLotID}], ProdID : [{OuProdID}]", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.2.4.3 [G4-3] Cell Output Report
        private int CELL_OUT_NG(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                string strModelID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_MODEL_ID.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;
                string strLineID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_LINE_ID.Trim();

                string strCellID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_CELL_ID"].AsString.Trim();
                ushort uNGCode = this.UNIT.Variables[$"{sender.Category.Name}:I_W_NG_TYPE"].AsShort;
                string strNGMsg = CellNgType(uNGCode);

                string strPalletID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_LOT_ID"].AsString.Trim().ToUpper();

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_PALLET_LOT_ID"} : {strPalletID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] Start - CellID : [{strCellID}]", strLogCategory, false, strEqpID, SHOPID.FORM);

                #region SubLot (NG Cell)
                List<CInSubLot> lstSubLot = new List<CInSubLot>();
                CInSubLot cInSubLot = new CInSubLot();
                cInSubLot.SUBLOTID = strCellID;
                cInSubLot.RESNCODE = uNGCode.ToString().Trim();
                cInSubLot.RESNDESC = strNGMsg;
                cInSubLot.DFCT_CELL_FLAG = "N";
                lstSubLot.Add(cInSubLot);
                #endregion

                iRet = bizBrFormRegPackingNgCell(false, strEqpID, txnID, strLineID, strSubModelID, strPalletID, lstSubLot, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                switch (iRet)
                {
                    case 0:
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }

                EIFLog(Level.Verbose, $"<======= {System.Reflection.MethodBase.GetCurrentMethod().Name} [{sender.Category.Name}] End - iRet : [{iRet}]", strLogCategory, false, strEqpID, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.2.4.6 [G4-6] Cell Information Request
        private int CELL_INFO_REQ(CVariable sender)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                EIFLog(Level.Verbose, $"[{sender.Category.Name}] Cell Information Request: On", strLogCategory, false, strEqpID, SHOPID.FORM);

                string strCellID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_CELL_ID"].AsString.Trim();

                iRet = 0;

                switch (iRet)
                {
                    case 0:
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }

            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.3.5.1 [S5-1] Processing Parameter Change Report
        private int MODEL_ID_CHG(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                string strModelID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_MODEL_ID"].AsString.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_MODEL_ID"} : {strModelID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                iRet = bizBrFormRegModelForEOL(false, strEqpID, txnID, strSubModelID, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                switch (iRet)
                {
                    case 0:
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }

            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }

        private int LINE_ID_CHG(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                EIFLog(Level.Verbose, $"[{sender.Category.Name}] Processing Parameter Change Report (Line ID) : On", strLogCategory, false, strEqpID, SHOPID.FORM);

                string strLineID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_LINE_ID"].AsString.Trim();

                SOLLog(Level.Info, $"[STEP_2] {$"{sender.Category.Name}:I_W_LINE_ID"} : {strLineID}", strLogCategory, strEqpID, SHOPID.FORM, txnID);

                iRet = bizBrPrdRegChangeFormLine(false, strEqpID, txnID, strLineID, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                switch (iRet)
                {
                    case 0:
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }

        private int FIRST_TRAY_USE_CHG(CVariable sender)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                EIFLog(Level.Verbose, $"[{sender.Category.Name}] Processing Parameter Change Report (First Tray) : On", strLogCategory, false, strEqpID, SHOPID.FORM);

                iRet = 0;

                switch (iRet)
                {
                    case 0:
                        break;

                    default:
                        SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                        break;
                }

            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.3.5.3 [S5-3] Processing Parameter Request
        private int PALET_INFO_REQ(CVariable sender, string txnID)
        {
            Exception BizRuleErr = null;
            int iRet = -1;

            try
            {
                string strModelID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_MODEL_ID.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;
                string strLineID = this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_LINE_ID.Trim();

                string strInputPalletID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_ID"].AsString.Trim().ToUpper();
                string strPstnID = this.UNIT.Variables[$"{sender.Category.Name}:I_W_PALLET_ID"].ConnectionInfo["PSTN_ID", string.Empty].ToString();

                string strScanRst = string.Empty;

                if (string.IsNullOrWhiteSpace(strInputPalletID) || Regex.IsMatch(strInputPalletID.ToUpper(), @"NO\s+READ|NOREAD"))
                {
                    SendHostAlarmMsg($"[{sender.Category.Name}] Carrier ID is Empty", HOST_ALM_TYPE.COMM_TYPE);

                    strScanRst = "NG";
                }
                else
                {
                    #region IN_PALLET
                    List<CInPallet> lstPallet = new List<CInPallet>();
                    CInPallet cInPallet = new CInPallet();
                    cInPallet.PALLETID = strInputPalletID;
                    lstPallet.Add(cInPallet);
                    #endregion

                    #region OutData
                    List<COutPallet> lstOutPallet = null;
                    #endregion

                    //BR_PRD_GET_PALLET_INFO_BY_CSTID
                    iRet = bizBrPrdGetPalletInfoByCstId(false, strEqpID, txnID, strLineID, strSubModelID, lstPallet, out lstOutPallet, out BizRuleErr); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                    switch (iRet)
                    {
                        case 0:
                            this.UNIT.Variables[$"{sender.Category.Name}:O_W_PALLET_LOT_ID"].AsString = lstOutPallet[0].HOST_PALLETID.ToUpper();
                            this.UNIT.Variables[$"{sender.Category.Name}:O_W_OUTPUT_RPT_STAT"].AsShort = (lstOutPallet[0].EQPT_END_FLAG.Equals(YESNO.Yes)) ? (ushort)EQPT_END_FLAG_CHK.COMPLETE : (ushort)EQPT_END_FLAG_CHK.NOT_COMPLETE;
                            this.UNIT.Variables[$"{sender.Category.Name}:O_W_OUTPUT_TYPE"].AsShort = PalletInOutTypeToInt(lstOutPallet[0].OUT_PALLET_TYPE);
                            break;

                        default:
                            SendHostAlarm(BizRuleErr, strLangID, HOST_ALM_TYPE.COMM_TYPE);
                            break;
                    }

                    strScanRst = "OK";
                }

                SendRfidReadingResult(sender, strPstnID, strScanRst, INOUT_TYPE.IN, strInputPalletID);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region 7.5.1.1 [T1-1] Port State Change Report
        private void PORT_STAT_CHG(CVariable sender, int iRptType)
        {
            Exception BizRuleErr = null;

            try
            {
                string strCarrierId = this.UNIT.Variables[$"{sender.Category.Name}:{CTag.I_W_TRIGGER_}CARRIER_ID"].AsString.Trim().ToUpper();
                string strPortID = this.UNIT.Variables[$"{sender.Category.Name}:{CTag.I_W_TRIGGER_}STAT"].ConnectionInfo["PSTN_ID", string.Empty].ToString();

                ushort iPortStat = this.UNIT.Variables[$"{sender.Category.Name}:{CTag.I_W_TRIGGER_}STAT"].AsShort;
                bool bCarrierExist = this.UNIT.Variables[$"{sender.Category.Name}:I_B_CARR_EXIST"].AsBoolean;

                string srtCstStat = this.UNIT.Variables[$"{sender.Category.Name}:I_W_TR_TYPE"].AsShort == 1 ? PORT_TRANSFER_CARRIER_STATE.USING : PORT_TRANSFER_CARRIER_STATE.EMPTY;

                string strTrgtEqpID = this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString; //JH 2025.06.23 HG 물류 통테 반영 TRGT_EQPT_ID에 설비 Machine ID (구본석 사원님 요청)

                if (iPortStat == PORT_STATE_TYPE.NONE) return;  //$ 2022.05.20 : 설비에서 Word값 변경 보고를 인식하기 위해 0으로 상태를 바꾸고 1,3,4,6,7을 주기 때문에 0은 Biz 처리 안함

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} <= PortState : [{iPortStat}] - 1:LR, 3:LC, 4:UR, 6:UC", strLogCategory, false, strEqpID, SHOPID.FORM);

                if (iPortStat == PORT_STATE_TYPE.LOAD_REQ || iPortStat == PORT_STATE_TYPE.UNLOAD_CMPLT)
                {
                    if (sender.Name.Equals($"{CTag.I_W_TRIGGER_}STAT"))
                        strCarrierId = string.Empty;
                    else return;
                }
                else if (iPortStat == PORT_STATE_TYPE.LOAD_CMPLT || iPortStat == PORT_STATE_TYPE.UNLOAD_REQ)
                {
                    if (string.IsNullOrWhiteSpace(strCarrierId)) return;
                }

                //$ 2022.09.20 : Packer에서 LR은 실 Pallet 요청일 때만 MHS Biz를 호출함(공 Pallet은 수동으로 투입하므로 반송 불필요 - FA 기술팀, 정경재 책임님) ->//JH 2025.06.23 HG 이후로는 CSTSTAT를 U,E 값 상관없이 MHS 호출한다고 하여 주석처리 김석찬 선임님 요청
                //if (srtCstStat == PORT_TRANSFER_CARRIER_STATE.USING)
                brMhsEifRegEqptPortTrfState(false, strEqpID, strPortID, iPortStat, strTrgtEqpID, strCarrierId, srtCstStat, string.Empty, out BizRuleErr);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }
        }

        private void PORT_OP_MODE_CHG(CVariable sender)
        {
            Exception BizRuleErr = null;

            try
            {
                string strPortID = this.UNIT.Variables[$"{sender.Category.Name}:{CTag.I_W_TRIGGER_}STAT"].ConnectionInfo["PSTN_ID", string.Empty].ToString();
                ushort uMode = this.UNIT.Variables[$"{sender.Category.Name}:{CTag.I_W_TRIGGER_}OP_MODE"].AsShort;
                string strMode = string.Empty;

                switch (uMode)
                {
                    case 1:
                    case 3:
                        strMode = PORT_MODE_2.AUTO;
                        break;
                    case 2:
                        strMode = PORT_MODE_2.MANUAL;
                        break;
                    default:
                        strMode = PORT_MODE_2.MANUAL;
                        break;
                }

                EIFLog(Level.Verbose, $"=======> {System.Reflection.MethodBase.GetCurrentMethod().Name} <= PortMode : [{strMode}] - A:Auto, M:Manual", strLogCategory, false, strEqpID, SHOPID.FORM);

                brMhsEifRegEqptPortAccessMode(false, strEqpID, strPortID, strMode, out BizRuleErr);

            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }
        }
        #endregion
        #endregion
        #endregion


        #region [ Biz Method ]
        #region Common
        public bool GetSystemTime(string TestMode, string EqptID, out DateTime SvrTime)
        {
            int iRet = -1;

            Exception BizRuleErr = null;
            CVariable InPara = null;
            CVariable OutPara = null;

            bool bResult = false;
            DateTime dtSvrTime = DateTime.Now;

            try
            {
                lock (objLockGetSystemTime)
                {
                    CBR_EQP_GET_SYSTEM_TIME_IN InData = CBR_EQP_GET_SYSTEM_TIME_IN.GetNew(this);

                    InData.IN_EQP_LENGTH = 1;

                    InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                    InData.IN_EQP[0].IFMODE = TestMode;
                    InData.IN_EQP[0].EQPTID = EqptID;
                    InData.IN_EQP[0].USERID = USERID.EIF;

                    CBR_EQP_GET_SYSTEM_TIME_OUT OutData = CBR_EQP_GET_SYSTEM_TIME_OUT.GetNew(this);

                    //iRet = _EIFServer.FAService.Request("BR_EQP_GET_SYSTEM_TIME", InData.Variable, OutData.Variable, out BizRuleErr, false);
                    iRet = BizCall("BR_EQP_GET_SYSTEM_TIME", EqptID, InData, OutData, out BizRuleErr, string.Empty, false); //$ 2024.11.27 : Solace Biz 전환

                    InPara = InData.Variable;
                    OutPara = OutData.Variable;
                    dtSvrTime = Convert.ToDateTime(OutData.OUT_EQP[0].SYSTIME);
                }
                if (iRet == 0)
                {
                    bResult = true;
                }
                else
                {
                    bResult = false;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, EqptID, SHOPID.FORM);
            }

            SvrTime = dtSvrTime;
            return bResult;
        }

        // [참조] Trouble Code(Alarm ID) 6자리 보고용 함수
        // 20180314 Trouble Code 6자리 대응
        public void RegEioState_V2(string EqptID, string EioState, string EIOLossCode, string TrblCode, bool bTestMode, string LotID, string EqptLotProgMode, string UnitEqptID = null, string UnitTrblCode = null)
        {
            int iRet = -1;

            Exception BizRuleErr = null;
            CVariable InPara = null;

            EIFLog(Level.Verbose, string.Format("Reg Eio State Start"), strLogCategory, false, EqptID, SHOPID.FORM);

            try
            {
                lock (objLockRegEioState)
                {

                    string strTroubleCode = TrblCode.PadLeft(6, '0');

                    CBR_SET_EQP_STATUS_IN InData = CBR_SET_EQP_STATUS_IN.GetNew(this);
                    CBR_SET_EQP_STATUS_OUT OutData = CBR_SET_EQP_STATUS_OUT.GetNew(this);

                    InData.IN_EQP_LENGTH = 1;

                    InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                    InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                    InData.IN_EQP[0].USERID = USERID.EIF;

                    InData.IN_EQP[0].EQPTID = EqptID;

                    InData.IN_EQP[0].EIOSTAT = EioState;

                    if (EioState == "T")
                    {
                        InData.IN_EQP[0].ALARM_ID = strTroubleCode;
                    }

                    if (EioState == "U" || EioState == "W") // HDH 2023.07.27 State W추가
                    {
                        if (this.EQPINFO__V_IS_SIXLOSSCODE_USE)  //$ 2023.07.26 : Loss Code 6자리 사용 시 
                            InData.IN_EQP[0].LOSS_CODE = EIOLossCode.PadLeft(6, '0'); //$ 2023.07.13 : 표준화팀 요청 LOSS Code 6자리 변경 
                        else
                            InData.IN_EQP[0].LOSS_CODE = EIOLossCode;
                    }

                    //iRet = _EIFServer.FAService.Request("BR_SET_EQP_STATUS", InData.Variable, OutData.Variable, out BizRuleErr);
                    iRet = BizCall("BR_SET_EQP_STATUS", EqptID, InData, OutData, out BizRuleErr); //$ 2024.11.27 : Solace Biz 전환

                    InPara = InData.Variable;
                }
                if (iRet == 0)
                {
                    EIFLog(Level.Verbose, string.Format("{0} : Success", "BR_SET_EQP_STATUS"), strLogCategory, false, EqptID, SHOPID.FORM);
                }
                else
                {
                    EIFLog(Level.Debug, string.Format("{0} : Fail", "BR_SET_EQP_STATUS"), strLogCategory, false, EqptID, SHOPID.FORM);

                    RegBizRuleException(bTestMode, EqptID, "BR_SET_EQP_STATUS", LotID, InPara, BizRuleErr);

                    //SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, EqptID, SHOPID.FORM);
            }
        }

        public void RegEqptSetAlarm(string EqptID, string txnID, string EioState, string TrblCode, bool bTestMode, bool bLogging)
        {
            int iRet = -1;

            Exception BizRuleErr = null;
            CVariable InPara = null;

            if (bLogging) EIFLog(Level.Verbose, string.Format("Reg Eqp Alarm Set Start"), strLogCategory, false, EqptID, SHOPID.FORM);

            try
            {
                lock (objLockRegAlarmSet)
                {

                    string strTroubleCode = TrblCode.PadLeft(6, '0');

                    CBR_EQP_REG_EQPT_ALARM_IN InData = CBR_EQP_REG_EQPT_ALARM_IN.GetNew(this);
                    InData.IN_EQP_LENGTH = 1;

                    InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                    InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                    InData.IN_EQP[0].USERID = USERID.EIF;

                    InData.IN_EQP[0].EQPTID = EqptID;
                    InData.IN_EQP[0].EIOSTAT = EioState;
                    InData.IN_EQP[0].EQPT_ALARM_CODE = strTroubleCode;
                    InData.IN_EQP[0].EQPT_ALARM_EVENT_TYPE = ALMTYPE.SET;

                    Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                    //iRet = _EIFServer.FAService.Request("BR_EQP_REG_EQPT_ALARM", InData.Variable, null, out BizRuleErr, bLogging);
                    iRet = BizCall("BR_EQP_REG_EQPT_ALARM", EqptID, InData, null, out BizRuleErr, txnID, bLogging); //$ 2024.11.27 : Solace Biz 전환

                    InPara = InData.Variable;
                }

                if (iRet == 0)
                {
                    if (bLogging) EIFLog(Level.Verbose, string.Format("{0} : Success", "BR_EQP_REG_EQPT_ALARM"), strLogCategory, false, EqptID, SHOPID.FORM);
                }
                else
                {
                    EIFLog(Level.Debug, string.Format("{0} : Fail", "BR_EQP_REG_EQPT_ALARM"), strLogCategory, false, EqptID, SHOPID.FORM);

                    RegBizRuleException(bTestMode, EqptID, "BR_EQP_REG_EQPT_ALARM", string.Empty, InPara, BizRuleErr);

                    SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, EqptID, SHOPID.FORM);
            }
        }

        public void RegEqptResetAlarm(string EqptID, string txnID, string EioState, string TrblCode, bool bTestMode, bool bLogging)
        {
            int iRet = -1;

            Exception BizRuleErr = null;
            CVariable InPara = null;

            if (bLogging) EIFLog(Level.Verbose, string.Format("Reg Eqp Alarm Reset Start"), strLogCategory, false, EqptID, SHOPID.FORM);

            try
            {
                lock (objLockRegAlarmReset)
                {

                    string strTroubleCode = TrblCode.PadLeft(6, '0');

                    CBR_EQP_REG_EQPT_ALARM_IN InData = CBR_EQP_REG_EQPT_ALARM_IN.GetNew(this);
                    InData.IN_EQP_LENGTH = 1;

                    InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                    InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                    InData.IN_EQP[0].USERID = USERID.EIF;

                    InData.IN_EQP[0].EQPTID = EqptID;
                    InData.IN_EQP[0].EIOSTAT = EioState;
                    InData.IN_EQP[0].EQPT_ALARM_CODE = strTroubleCode;

                    // RESET시 ALARMID가 0인 경우 EQPT_ALARM_EVENT_TYPE은 값을 Mapping하지 않게 하여 NULL로 인식할 수 있게 하자.
                    if (int.Parse(TrblCode) > 0)
                        InData.IN_EQP[0].EQPT_ALARM_EVENT_TYPE = ALMTYPE.RESET;

                    Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                    //iRet = _EIFServer.FAService.Request("BR_EQP_REG_EQPT_ALARM", InData.Variable, null, out BizRuleErr, bLogging);
                    iRet = BizCall("BR_EQP_REG_EQPT_ALARM", EqptID, InData, null, out BizRuleErr, txnID, bLogging); //$ 2024.11.27 : Solace Biz 전환

                    InPara = InData.Variable;
                }

                if (iRet == 0)
                {
                    if (bLogging) EIFLog(Level.Verbose, string.Format("{0} : Success", "BR_EQP_REG_EQPT_ALARM"), strLogCategory, false, EqptID, SHOPID.FORM);
                }
                else
                {
                    EIFLog(Level.Debug, string.Format("{0} : Fail", "BR_EQP_REG_EQPT_ALARM"), strLogCategory, false, EqptID, SHOPID.FORM);

                    RegBizRuleException(bTestMode, EqptID, "BR_EQP_REG_EQPT_ALARM", string.Empty, InPara, BizRuleErr);

                    SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, EqptID, SHOPID.FORM);
            }
        }

        public void RegSmokeDetectREQ(string EqptID, string SmokeStatus, bool bTestMode)
        {
            int iRet = -1;

            Exception BizRuleErr = null;
            CVariable InPara = null;

            EIFLog(Level.Verbose, string.Format("Reg SmokeDetect Start"), strLogCategory, false, EqptID, SHOPID.FORM);

            try
            {
                lock (objLockRegSmokeDetect)
                {

                    CBR_SET_FORM_FIRE_OCCUR_NEW_IN InData = CBR_SET_FORM_FIRE_OCCUR_NEW_IN.GetNew(this);
                    CBR_SET_FORM_FIRE_OCCUR_NEW_OUT outData = CBR_SET_FORM_FIRE_OCCUR_NEW_OUT.GetNew(this);

                    InData.INDATA_LENGTH = 1;

                    InData.INDATA[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                    InData.INDATA[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                    InData.INDATA[0].USERID = USERID.EIF;

                    InData.INDATA[0].EQPTID = EqptID;
                    InData.INDATA[0].SMOKE_DETECT = SmokeStatus;
                    //InData.INDATA[0].TRAY_EXIST = ????; //TODO : TrayExist가 필요하다면 추가 작업 필요

                    Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                    //iRet = _EIFServer.FAService.Request("BR_SET_FORM_FIRE_OCCUR_NEW", InData.Variable, null, out BizRuleErr);                    
                    iRet = BizCall("BR_SET_FORM_FIRE_OCCUR_NEW", EqptID, InData, null, out BizRuleErr); //$ 2024.11.27 : Solace Biz 전환

                    InPara = InData.Variable;
                }

                if (iRet == 0)
                {
                    EIFLog(Level.Verbose, string.Format("{0} : Success", "BR_SET_FORM_FIRE_OCCUR_NEW"), strLogCategory, false, EqptID, SHOPID.FORM);
                }
                else
                {
                    EIFLog(Level.Debug, string.Format("{0} : Fail", "BR_SET_FORM_FIRE_OCCUR_NEW"), strLogCategory, false, EqptID, SHOPID.FORM);

                    RegBizRuleException(bTestMode, EqptID, "BR_SET_FORM_FIRE_OCCUR_NEW", string.Empty, InPara, BizRuleErr);

                    //SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, EqptID, SHOPID.FORM);
            }
        }

        public void RegBizRuleException(bool bTestMode, string EqptID, string BizRuleName, string LotID, CVariable InPara, Exception BizRuleErr)
        {
            int iRet = -1;

            string strInData = string.Empty;

            EIFLog(Level.Verbose, string.Format("BizRule Exception Save Start"), strLogCategory, false, EqptID, SHOPID.FORM);

            try
            {
                lock (objLockRegBizRuleErr)
                {
                    CBR_SYS_REG_BIZRULE_EXCEPTION_IN InData = CBR_SYS_REG_BIZRULE_EXCEPTION_IN.GetNew(this);

                    InData.IN_EQP_LENGTH = 1;

                    InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                    InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                    InData.IN_EQP[0].EQPTID = EqptID;
                    InData.IN_EQP[0].PROGRAMID = this.Parent.Name;
                    InData.IN_EQP[0].BIZRULEID = BizRuleName;

                    if (BizRuleErr != null)
                    {
                        InData.IN_EQP[0].EXCEPTION_MESSAGE = BizRuleErr.Message.ToString();

                        if (BizRuleErr.Data != null)
                        {
                            InData.IN_EQP[0].EXCEPTION_CODE = BizRuleErr.Data.Contains("CODE") ? BizRuleErr.Data["CODE"].ToString() : string.Empty;
                            InData.IN_EQP[0].EXCEPTION_TYPE = BizRuleErr.Data.Contains("TYPE") ? BizRuleErr.Data["TYPE"].ToString() : string.Empty;
                            InData.IN_EQP[0].EXCEPTION_LOC = BizRuleErr.Data.Contains("LOC") ? BizRuleErr.Data["LOC"].ToString() : string.Empty;

                            if (BizRuleErr.Data.Contains("TYPE") && BizRuleErr.Data["TYPE"].ToString().Equals("USER"))
                            {
                                InData.IN_EQP[0].EXCEPTION_DATA = BizRuleErr.Data.Contains("DATA") ? BizRuleErr.Data["DATA"].ToString() : string.Empty;
                            }
                            else
                            {
                                InData.IN_EQP[0].EXCEPTION_DATA = string.Empty;
                            }

                            InData.IN_EQP[0].EXCEPTION_PARA = BizRuleErr.Data.Contains("PARA") ? BizRuleErr.Data["PARA"].ToString() : string.Empty;
                        }
                    }

                    InData.IN_EQP[0].LOTID = LotID;
                    InData.IN_EQP[0].DATASET = SetErrDataSet(InPara);

                    //iRet = _EIFServer.FAService.Request("BR_SYS_REG_BIZRULE_EXCEPTION", InData.Variable, null);
                    iRet = BizCall("BR_SYS_REG_BIZRULE_EXCEPTION", EqptID, InData, null, out BizRuleErr); //$ 2024.11.27 : Solace Biz 전환
                }

                if (iRet == 0)
                {
                    EIFLog(Level.Verbose, string.Format("{0} : Success : {1}", "BR_SYS_REG_BIZRULE_EXCEPTION", strInData), strLogCategory, false, EqptID, SHOPID.FORM);
                }
                else
                {
                    EIFLog(Level.Debug, string.Format("{0} : Fail", "BR_SYS_REG_BIZRULE_EXCEPTION"), strLogCategory, false, EqptID, SHOPID.FORM);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, EqptID, SHOPID.FORM);
            }
        }

        public void RegEqptOperInfo(bool bTestMode, string EqptID, string DayNightCode, List<double> TimeData, string CellID = "")
        {
            int iRet = -1;

            Exception BizRuleErr = null;
            CVariable InPara = null;

            double dblSumTimeData = 0;

            foreach (double time in TimeData)
            {
                dblSumTimeData += time;
            }

            // $2022.09.29 Tact Time 0이어도 Tact Time 수집 가능 하도록 주석 처리
            //if (dblSumTimeData == 0)
            //{
            //    EIFLog(Level.Debug, string.Format("Reg Equipment Operation Information Clct Data is 0"), strLogCategory, false, EqptID, SHOPID.FORM);

            //    return;
            //}

            EIFLog(Level.Verbose, string.Format("Reg Equipment Operation Information Clct Data Save Start"), strLogCategory, false, EqptID, SHOPID.FORM);

            try
            {
                lock (objLockOperInfo)
                {
                    CBR_EQP_REG_EQPT_OPER_INFO_IN InData = CBR_EQP_REG_EQPT_OPER_INFO_IN.GetNew(this);

                    InData.IN_EQP_LENGTH = 1;

                    InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                    InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                    InData.IN_EQP[0].EQPTID = EqptID;
                    InData.IN_EQP[0].DAYNIGHT_TYPE_CODE = DayNightCode;
                    InData.IN_EQP[0].OPER_TIME = TimeData[0];
                    InData.IN_EQP[0].WAIT_TIME = TimeData[1];
                    InData.IN_EQP[0].TRBL_TIME = TimeData[2];
                    InData.IN_EQP[0].USER_STOP_TIME = TimeData[3];
                    InData.IN_EQP[0].TACT_TIME = TimeData[4];
                    InData.IN_EQP[0].PPM_VALUE = TimeData[5];
                    InData.IN_EQP[0].CELLID = CellID;  //$ 2022.10.04 : Tact Time 보고 시 Degas/EOL은 CellID가 필요하다고 함

                    //iRet = _EIFServer.FAService.Request("BR_EQP_REG_EQPT_OPER_INFO", InData.Variable, null, out BizRuleErr);
                    iRet = BizCall("BR_EQP_REG_EQPT_OPER_INFO", EqptID, InData, null, out BizRuleErr); //$ 2024.11.27 : Solace Biz 전환

                    InPara = InData.Variable;
                }
                if (iRet == 0)
                {
                    EIFLog(Level.Verbose, string.Format("{0} : Success", "BR_EQP_REG_EQPT_OPER_INFO"), strLogCategory, false, EqptID, SHOPID.FORM);
                }
                else
                {
                    EIFLog(Level.Debug, string.Format("{0} : Fail", "BR_EQP_REG_EQPT_OPER_INFO"), strLogCategory, false, EqptID, SHOPID.FORM);

                    RegBizRuleException(bTestMode, EqptID, "BR_EQP_REG_EQPT_OPER_INFO", string.Empty, InPara, BizRuleErr);

                    //SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, EqptID, SHOPID.FORM);
            }
        }

        //20191230 RFID 리딩율 모니터링
        public void RegPrdRegEqptScanRst(bool bTestMode, string EqptID, CInScan cInScan)
        {
            // (2020.2.25) CWA MES에서 RSLT=OK인 경우도 Biz 호출 요청하였음
            //if (cInScan.SCAN_TYPE.Equals("F") && cInScan.SCAN_RSLT.Equals("OK")) { return; }
            int iRet = -1;

            Exception BizRuleErr = null;
            CVariable InPara = null;

            EIFLog(Level.Verbose, string.Format("Eqpt Scan Data Save Start"), strLogCategory, false, EqptID, SHOPID.FORM);

            try
            {
                CBR_PRD_REG_EQPT_SCAN_RSLT_IN InData = CBR_PRD_REG_EQPT_SCAN_RSLT_IN.GetNew(this);

                InData.IN_EQP_LENGTH = 1;

                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = EqptID;
                InData.IN_EQP[0].USERID = USERID.EIF;

                InData.IN_SCAN_LENGTH = 1;

                InData.IN_SCAN[0].IN_OUT_TYPE = cInScan.IN_OUT_TYPE;
                InData.IN_SCAN[0].EQPT_MOUNT_PSTN_ID = cInScan.EQPT_MOUNT_PSTN_ID;
                InData.IN_SCAN[0].SCAN_TYPE = cInScan.SCAN_TYPE;
                InData.IN_SCAN[0].SCAN_RSLT = cInScan.SCAN_RSLT;
                InData.IN_SCAN[0].CSTID = cInScan.CSTID;
                InData.IN_SCAN[0].CST_LOAD_LAYER_CODE = cInScan.CST_LOAD_LAYER_CODE;

                //iRet = _EIFServer.FAService.Request("BR_PRD_REG_EQPT_SCAN_RSLT", InData.Variable, null, out BizRuleErr);
                iRet = BizCall("BR_PRD_REG_EQPT_SCAN_RSLT", EqptID, InData, null, out BizRuleErr); //$ 2024.11.27 : Solace Biz 전환

                InPara = InData.Variable;

                if (iRet == 0)
                {
                    EIFLog(Level.Verbose, string.Format("{0} : Success", "BR_PRD_REG_EQPT_SCAN_RSLT"), strLogCategory, false, EqptID, SHOPID.FORM);
                }
                else
                {
                    EIFLog(Level.Debug, string.Format("{0} : Fail", "BR_PRD_REG_EQPT_SCAN_RSLT"), strLogCategory, false, EqptID, SHOPID.FORM);

                    RegBizRuleException(bTestMode, EqptID, "BR_PRD_REG_EQPT_SCAN_RSLT", string.Empty, InPara, BizRuleErr);

                    //SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
        }

        #region 물류 Biz        
        public int brMhsEifRegEqptPortTrfState(bool bTestMode, string strEqpID, string strPortID, ushort iPortStat, string strTrgtEqptID, string strSkid, string strCstStat, string strEltrType, out Exception BizRuleErr)
        {

            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            string strPortStat = string.Empty;

            switch (iPortStat)
            {
                case PORT_STATE_TYPE.NONE:
                    strPortStat = "NA";
                    break;
                case PORT_STATE_TYPE.LOAD_REQ:
                    strPortStat = "LR";
                    break;
                case PORT_STATE_TYPE.LOAD_CMPLT:
                    strPortStat = "LC";
                    break;
                case PORT_STATE_TYPE.UNLOAD_REQ:
                    strPortStat = "UR";
                    break;
                case PORT_STATE_TYPE.UNLOAD_CMPLT:
                    strPortStat = "UC";
                    break;
                case PORT_STATE_TYPE.PORT_LOCK:
                    strPortStat = "PL";
                    break;
                default:
                    return -1;
            }

            try
            {
                string strBizName = "BR_MHS_EIF_REG_EQPT_PORT_TRF_STATE";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_MHS_EIF_REG_EQPT_PORT_TRF_STATE_IN InData = CBR_MHS_EIF_REG_EQPT_PORT_TRF_STATE_IN.GetNew(this);

                #region IN_EQP
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID.Trim();
                InData.IN_EQP[0].USERID = USERID.EIF;
                #endregion

                #region IN_PORT
                InData.IN_PORT_LENGTH = 1;
                InData.IN_PORT[0].PORT_ID = strPortID;
                InData.IN_PORT[0].PORT_STAT_CODE = strPortStat;
                InData.IN_PORT[0].TRGT_EQPT_ID = strTrgtEqptID;
                InData.IN_PORT[0].CSTID = strSkid;
                InData.IN_PORT[0].CSTSTAT = strCstStat;
                InData.IN_PORT[0].ELTR_TYPE_CODE = strEltrType;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, null, out BizRuleErr);                
                iRet = BizCall(strBizName, strEqpID, InData, null, out BizRuleErr); //$ 2024.11.27 : Solace Biz 전환

                InPara = InData.Variable;
                switch (iRet)
                {
                    case 0:

                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;

                    default:

                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        RegBizRuleException(false, strEqpID, strBizName, strPortID, InPara, BizRuleErr);
                        //SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }
            return iRet;
        }

        public int brMhsEifRegEqptPortAccessMode(bool bTestMode, string strEqpID, string strPortID, string strAccessMode, out Exception BizRuleErr)
        {

            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            try
            {
                string strBizName = "BR_MHS_EIF_REG_EQPT_PORT_ACCESS_MODE";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_MHS_EIF_REG_EQPT_PORT_ACCESS_MODE_IN InData = CBR_MHS_EIF_REG_EQPT_PORT_ACCESS_MODE_IN.GetNew(this);

                #region IN_EQP
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID.Trim();
                InData.IN_EQP[0].USERID = USERID.EIF;
                #endregion

                #region IN_PORT
                InData.IN_PORT_LENGTH = 1;
                InData.IN_PORT[0].PORT_ID = strPortID;
                InData.IN_PORT[0].ACCESS_MODE_CODE = strAccessMode;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, null, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, null, out BizRuleErr); //$ 2024.11.27 : Solace Biz 전환

                InPara = InData.Variable;
                switch (iRet)
                {
                    case 0:

                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;

                    default:

                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        RegBizRuleException(false, strEqpID, strBizName, strPortID, InPara, BizRuleErr);
                        //SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }
            return iRet;
        }

        //$ 2022.12.21 : 설비로 투입되는 Tray에 대해 TrayID를 MHS로 보고하여 반송 종료 처리 함
        public int brMhsEifRegEqptLoadedPallet(bool bTestMode, string strEqpID, string strPortID, string strSkid, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            try
            {
                string strBizName = "BR_MHS_EIF_REG_REPORT_LOADED_CSTID";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_MHS_EIF_REG_REPORT_LOADED_CSTID_IN InData = CBR_MHS_EIF_REG_REPORT_LOADED_CSTID_IN.GetNew(this);

                #region IN_EQP
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID.Trim();
                InData.IN_EQP[0].PORT_ID = strPortID.Trim();
                InData.IN_EQP[0].USERID = USERID.EIF;
                #endregion

                #region IN_PORT
                InData.IN_CST_LENGTH = 1;
                InData.IN_CST[0].CSTID = strSkid;
                InData.IN_CST[0].STACK_NO = "1";
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, null, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, null, out BizRuleErr); //$ 2024.11.27 : Solace Biz 전환

                InPara = InData.Variable;
                switch (iRet)
                {
                    case 0:

                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;

                    default:

                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        RegBizRuleException(false, strEqpID, strBizName, strPortID, InPara, BizRuleErr);
                        //SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }
            return iRet;
        }
        #endregion
        #endregion

        #region BR_EQP_REG_EQPT_WORK_EVENT_EMS_TO_MQ
        // JH 2025.06.13 재료 교체 알람 추가 [UC2 반영] 및 EIF 4.0 전환
        public int BrEqpRegEqptWorkEventEMSToMq(bool bTestMode, string strEqptID, string strLotID, string strEventCode, out Exception BizRuleErr)
        {
            int iRet = -1;
            BizRuleErr = null;
            CVariable InPara = null;

            try
            {
                DateTime now = DateTime.Now;
                string strBizName = "BR_EQP_REG_EQPT_WORK_EVENT_EMS_TO_MQ";
                string strDateTime = now.ToString("yyyy-MM-dd HH:mm:ss");

                CBR_EQP_REG_EQPT_WORK_EVENT_EMS_TO_MQ_IN InData = CBR_EQP_REG_EQPT_WORK_EVENT_EMS_TO_MQ_IN.GetNew(this);

                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EVENTCODE = strEventCode;
                InData.IN_EQP[0].EVENTNAME = string.Empty;
                InData.IN_EQP[0].ACTDTM = strDateTime;
                InData.IN_EQP[0].EQPTID = strEqptID;
                InData.IN_EQP[0].LOTID = strLotID;
                InData.IN_EQP[0].USERID = USERID.EIF;

                iRet = BizCall(strBizName, strEqptID, InData, null, out BizRuleErr); //$ 2024.11.27 : Solace Biz 전환

                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqptID, SHOPID.ASSY);
                        break;

                    default:
                        EIFLog(Level.Verbose, string.Format("[{0}] Fail \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqptID, SHOPID.ASSY);
                        RegBizRuleException(false, strEqptID, strBizName, InData.IN_EQP.ToString(), InPara, BizRuleErr);
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqptID, SHOPID.ASSY);
            }
            return iRet;
        }
        #endregion

        #region BR_PRD_REG_CHANGE_FORM_LINE
        protected int bizBrPrdRegChangeFormLine(bool bTestMode, string strEqpID, string txnID, string FormLineID, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            try
            {
                string strBizName = "BR_PRD_REG_CHANGE_FORM_LINE";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_PRD_REG_CHANGE_FORM_LINE_IN InData = CBR_PRD_REG_CHANGE_FORM_LINE_IN.GetNew(this);

                #region Equipment
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID;
                InData.IN_EQP[0].USERID = USERID.EIF;
                InData.IN_EQP[0].FORM_LINEID = FormLineID;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, null, out BizRuleErr);                
                iRet = BizCall(strBizName, strEqpID, InData, null, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }

            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_PRD_REG_PACKING_VISION_JUDG
        protected int bizBrRegPackingVisionJudb(bool bTestMode, string strEqpID, string txnID, string strCellID, Dictionary<string, string> dicJudge, out Exception BizRuleErr)
        {

            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            try
            {
                string strBizName = "BR_PRD_REG_PACKING_VISION_JUDG";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_PRD_REG_PACKING_VISION_JUDG_IN InData = CBR_PRD_REG_PACKING_VISION_JUDG_IN.GetNew(this);

                #region Equipment
                InData.IN_EQPT_LENGTH = 1;
                InData.IN_EQPT[0].EQPTID = strEqpID;
                InData.IN_EQPT[0].USERID = USERID.EIF;
                #endregion

                #region Sublot
                InData.IN_SUBLOT_LENGTH = 1;
                InData.IN_SUBLOT[0].SUBLOTID = strCellID;
                #endregion

                #region Clct Item
                InData.IN_CLCT_LENGTH = dicJudge.Count;

                int idx = 0;
                foreach (KeyValuePair<string, string> Cell in dicJudge)
                {
                    InData.IN_CLCT[idx].CLCTITEM = Cell.Key;
                    InData.IN_CLCT[idx].CLCTVALUE = Cell.Value;

                    idx++;
                }
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, null, out BizRuleErr, false);
                iRet = BizCall(strBizName, strEqpID, InData, null, out BizRuleErr, txnID, false); //$ 2024.11.27 : Solace Biz 전환, //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        //EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_GET_NEW_PACKING_PALLETID
        protected int bizBrFormGetNewPackingPalletID(bool bTestMode, string strEqpID, string txnID, string FormLineID, string ModelID, string PalletRfid, out string PalletID, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            PalletID = string.Empty;

            try
            {
                string strBizName = "BR_FORM_GET_NEW_PACKING_PALLETID";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_FORM_GET_NEW_PACKING_PALLETID_IN InData = CBR_FORM_GET_NEW_PACKING_PALLETID_IN.GetNew(this);
                CBR_FORM_GET_NEW_PACKING_PALLETID_OUT OutData = CBR_FORM_GET_NEW_PACKING_PALLETID_OUT.GetNew(this);

                #region Equipment
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID;
                InData.IN_EQP[0].USERID = USERID.EIF;
                InData.IN_EQP[0].MODELID = ModelID;
                InData.IN_EQP[0].PALLET_RFID = PalletRfid;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, OutData.Variable, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, OutData, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        PalletID = OutData.OUT_PACK[0].PALLETID;

                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);

                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }

            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_REG_START_PACKING_PALLET
        protected int bizBrFormRegStartPackingPallet(bool bTestMode, string strEqpID, string txnID, string FormLineID, string ModelID, string PalletID, out string BoxID, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            BoxID = string.Empty;

            try
            {
                string strBizName = "BR_FORM_REG_START_PACKING_PALLET";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_FORM_REG_START_PACKING_PALLET_IN InData = CBR_FORM_REG_START_PACKING_PALLET_IN.GetNew(this);
                CBR_FORM_REG_START_PACKING_PALLET_OUT OutData = CBR_FORM_REG_START_PACKING_PALLET_OUT.GetNew(this);

                #region Equipment

                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID;
                InData.IN_EQP[0].USERID = USERID.EIF;
                InData.IN_EQP[0].FORM_LINEID = FormLineID;
                #endregion

                #region Pallet
                InData.IN_PALLET_LENGTH = 1;
                InData.IN_PALLET[0].PALLETID = PalletID;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, OutData.Variable, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, OutData, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        int iOutBoxCnt = OutData.OUT_BOX_LENGTH;

                        if (iOutBoxCnt > 0)
                            BoxID = OutData.OUT_BOX[0].BOXID;
                        else
                            EIFLog(Level.Verbose, string.Format("[{0}] OUT_BOX_LENGTH <= 0", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }

            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_GET_NEW_PACKING_BOXID
        protected int bizBrFormGetNewPackingBoxID(bool bTestMode, string strEqpID, string txnID, string FormLineID, string ModelID, string BoxRfid, out string BoxID, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            BoxID = string.Empty;

            try
            {
                string strBizName = "BR_FORM_GET_NEW_PACKING_BOXID";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_FORM_GET_NEW_PACKING_BOXID_IN InData = CBR_FORM_GET_NEW_PACKING_BOXID_IN.GetNew(this);
                CBR_FORM_GET_NEW_PACKING_BOXID_OUT OutData = CBR_FORM_GET_NEW_PACKING_BOXID_OUT.GetNew(this);

                #region Equipment
                //$ 2024.12.19 |tlsrmsdl1| : MES2.0 리빌딩 INDATA -> FORM_LINEID 삭제 ,MODELID 추가로 인해 수정 진행
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID;
                InData.IN_EQP[0].USERID = USERID.EIF;
                InData.IN_EQP[0].MODELID = ModelID;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, OutData.Variable, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, OutData, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        int iOutBoxCnt = OutData.OUT_BOX_LENGTH;

                        if (iOutBoxCnt > 0)
                            BoxID = OutData.OUT_BOX[0].BOXID;
                        else
                            EIFLog(Level.Verbose, string.Format("[{0}] OUT_BOX_LENGTH <= 0", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }

            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_REG_START_PACKING_BOX
        protected int bizBrFormRegStartPackingBox(bool bTestMode, string strEqpID, string txnID, string FormLineID, string ModelID, string PalletID, string BoxID, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            try
            {
                string strBizName = "BR_FORM_REG_START_PACKING_BOX";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_FORM_REG_START_PACKING_BOX_IN InData = CBR_FORM_REG_START_PACKING_BOX_IN.GetNew(this);

                #region Equipment
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID;
                InData.IN_EQP[0].USERID = USERID.EIF;
                InData.IN_EQP[0].FORM_LINEID = FormLineID;
                InData.IN_EQP[0].MODELID = ModelID;
                #endregion

                #region In_Pallet
                InData.IN_PALLET_LENGTH = 1;
                InData.IN_PALLET[0].PALLETID = PalletID;
                InData.IN_PALLET[0].BOXID = BoxID;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, null, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, null, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }

            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_CHK_PACKING_CELL
        protected int bizBrFormChkPackingCell(bool bTestMode, string strEqpID, string txnID, string FormLineID, string ModelID, string SubLotID, out string LotID, out string ProdID, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;
            CVariable OutPara = null;

            LotID = string.Empty;
            ProdID = string.Empty;

            try
            {
                string strBizName = "BR_FORM_CHK_PACKING_CELL";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_FORM_CHK_PACKING_CELL_IN InData = CBR_FORM_CHK_PACKING_CELL_IN.GetNew(this);
                CBR_FORM_CHK_PACKING_CELL_OUT OutData = CBR_FORM_CHK_PACKING_CELL_OUT.GetNew(this);

                #region Equipment
                //$ 2024.12.19 |tlsrmsdl1| : M2S2.0 리빌딩 INDATA IFMODE 추가로 인해 수정진행
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID;
                InData.IN_EQP[0].USERID = USERID.EIF;
                InData.IN_EQP[0].MODELID = ModelID;
                #endregion

                #region SubLot
                InData.IN_SUBLOT_LENGTH = 1;
                InData.IN_SUBLOT[0].SUBLOTID = SubLotID;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, OutData.Variable, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, OutData, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환
                InPara = InData.Variable;
                OutPara = OutData.Variable;

                switch (iRet)
                {
                    case 0:
                        int iOutSubLotCnt = OutData.OUT_SUBLOT_LENGTH;

                        if (iOutSubLotCnt > 0)
                        {
                            LotID = OutData.OUT_SUBLOT[0].LOTID;
                            ProdID = OutData.OUT_SUBLOT[0].PRODID;
                        }
                        else
                            EIFLog(Level.Verbose, string.Format("[{0}] OUT_SUBLOT_LENGTH <= 0", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1} {2}", strBizName, InPara.ToString(), OutPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_CHK_VISION_JUDG_CELL
        protected int bizBrFormChkVisionJudgCell(bool bTestMode, string strEqpID, string txnID, string FormLineID, string ModelID, string SubLotID, string VisionJudg001, string VisionJudg002, string VisionJudg003, string VisionJudg004, string VisionJudg005, string VisionJudg006, string VisionJudg007, string VisionJudg008, string VisionJudg009, string VisionJudg010, string VisionJudg011, string VisionJudg012, string VisionJudg013, out int NGCode, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;
            CVariable OutPara = null;

            NGCode = 0;

            try
            {
                string strBizName = "BR_PRD_REG_PACKING_VISION_JUDG_PRIORITY";

                EIFLog(Level.Verbose, $"[{strBizName}] Start", strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_PRD_REG_PACKING_VISION_JUDG_PRIORITY_IN InData = CBR_PRD_REG_PACKING_VISION_JUDG_PRIORITY_IN.GetNew(this);
                CBR_PRD_REG_PACKING_VISION_JUDG_PRIORITY_OUT OutData = CBR_PRD_REG_PACKING_VISION_JUDG_PRIORITY_OUT.GetNew(this);

                #region Indata
                InData.IN_EQPT_LENGTH = 1;
                InData.IN_EQPT[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQPT[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQPT[0].USERID = USERID.EIF;
                InData.IN_EQPT[0].EQPTID = strEqpID;

                InData.IN_SUBLOT_LENGTH = 1;
                InData.IN_SUBLOT[0].SUBLOTID = SubLotID;

                InData.IN_NG_LENGTH = 1;
                InData.IN_NG[0].VISION_JUDG_001 = VisionJudg001;
                InData.IN_NG[0].VISION_JUDG_002 = VisionJudg002;
                InData.IN_NG[0].VISION_JUDG_003 = VisionJudg003;
                InData.IN_NG[0].VISION_JUDG_004 = VisionJudg004;
                InData.IN_NG[0].VISION_JUDG_005 = VisionJudg005;
                InData.IN_NG[0].VISION_JUDG_006 = VisionJudg006;
                InData.IN_NG[0].VISION_JUDG_007 = VisionJudg007;
                InData.IN_NG[0].VISION_JUDG_008 = VisionJudg008;
                InData.IN_NG[0].VISION_JUDG_009 = VisionJudg009;
                InData.IN_NG[0].VISION_JUDG_010 = VisionJudg010;
                InData.IN_NG[0].VISION_JUDG_011 = VisionJudg011;
                InData.IN_NG[0].VISION_JUDG_012 = VisionJudg012;
                InData.IN_NG[0].VISION_JUDG_013 = VisionJudg013;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, OutData.Variable, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, OutData, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;
                OutPara = OutData.Variable;

                switch (iRet)
                {
                    case 0:
                        NGCode = OutData.OUTDATA[0].NG_CODE;
                        EIFLog(Level.Verbose, $"[{strBizName}] Success \n{InPara.ToString()}{OutPara.ToString()}", strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;

                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, $"[{strBizName}] Fail - {BizRuleErr.Message} \n{InPara.ToString()}", strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_REG_PACKING_NG_CELL
        protected int bizBrFormRegPackingNgCell(bool bTestMode, string strEqpID, string txnID, string FormLineID, string ModelID, string PalletID, List<CInSubLot> lstSubLot, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            try
            {
                string strBizName = "BR_FORM_REG_PACKING_NG_CELL";
                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_FORM_REG_PACKING_NG_CELL_IN InData = CBR_FORM_REG_PACKING_NG_CELL_IN.GetNew(this);

                #region Equipment
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID;
                InData.IN_EQP[0].USERID = USERID.EIF;
                #endregion

                #region IN_PALLET
                InData.IN_PALLET_LENGTH = 1;
                InData.IN_PALLET[0].PALLETID = PalletID;
                #endregion

                #region SubLot
                InData.IN_SUBLOT_LENGTH = lstSubLot.Count;

                for (int i = 0; i < lstSubLot.Count; i++)
                {
                    InData.IN_SUBLOT[i].SUBLOTID = lstSubLot[i].SUBLOTID;
                    InData.IN_SUBLOT[i].RESNCODE = lstSubLot[i].RESNCODE;
                    InData.IN_SUBLOT[i].RESNDESC = lstSubLot[i].RESNDESC;
                }
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, null, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, null, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_REG_END_PACKING_BOX
        protected int bizBrFormRegEndPackingBox(bool bTestMode, string strEqpID, string txnID, string FormLineID, string ModelID, List<CInPallet> lstPallet, List<CInBox> lstBox, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            try
            {
                string strBizName = "BR_FORM_REG_END_PACKING_BOX";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_FORM_REG_END_PACKING_BOX_IN InData = CBR_FORM_REG_END_PACKING_BOX_IN.GetNew(this);

                #region Equipment
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID;
                InData.IN_EQP[0].USERID = USERID.EIF;
                InData.IN_EQP[0].FORM_LINEID = FormLineID;
                InData.IN_EQP[0].MODELID = ModelID;
                #endregion

                #region Pallet
                InData.IN_PALLET_LENGTH = lstPallet.Count;

                for (int i = 0; i < lstPallet.Count; i++)
                {
                    InData.IN_PALLET[0].PALLETID = lstPallet[i].PALLETID;
                    InData.IN_PALLET[0].BOXID = lstPallet[i].BOXID;
                    InData.IN_PALLET[0].PACKING_QTY = lstPallet[i].PACKING_QTY;
                    InData.IN_PALLET[0].EMPTY_TRAY_FLAG = lstPallet[i].EMPTY_TRAY_FLAG;
                }
                #endregion

                #region Box
                InData.IN_BOX_LENGTH = lstBox.Count;

                for (int i = 0; i < lstBox.Count; i++)
                {
                    InData.IN_BOX[i].PSTN_NO = lstBox[i].PSTN_NO;
                    InData.IN_BOX[i].SUBLOTID = lstBox[i].SUBLOTID;
                }
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, null, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, null, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_REG_END_PACKING_PALLET
        protected int bizBrFormRegEndPackingPallet(bool bTestMode, string strEqpID, string txnID, string FormLineID, string ModelID, List<CInPallet> lstPallet, List<CInBox> lstBox, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            try
            {
                string strBizName = "BR_FORM_REG_END_PACKING_PALLET";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_FORM_REG_END_PACKING_PALLET_IN InData = CBR_FORM_REG_END_PACKING_PALLET_IN.GetNew(this);

                #region Equipment
                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.IN_EQP[0].EQPTID = strEqpID;
                //$ 2024.12.19 |tlsrmsdl1| : INDATA -> SRCTYPE,IFMODE 추가로 인해 수정진행
                InData.IN_EQP[0].USERID = USERID.EIF;
                InData.IN_EQP[0].MODELID = ModelID;
                #endregion

                #region Pallet
                InData.IN_PALLET_LENGTH = lstPallet.Count;

                for (int i = 0; i < lstPallet.Count; i++)
                {
                    InData.IN_PALLET[0].PALLETID = lstPallet[i].PALLETID;
                    InData.IN_PALLET[0].PACKING_QTY = lstPallet[i].PACKING_QTY;
                }
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, null, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, null, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_REG_MODEL_FOR_EOL
        protected int bizBrFormRegModelForEOL(bool bTestMode, string strEqpID, string txnID, string strModelID, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;

            try
            {
                string strBizName = "BR_FORM_REG_MODEL_FOR_EOL";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_FORM_REG_MODEL_FOR_EOL_IN InData = CBR_FORM_REG_MODEL_FOR_EOL_IN.GetNew(this);

                #region Indata
                InData.INDATA_LENGTH = 1;
                InData.INDATA[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.INDATA[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.INDATA[0].EQPTID = strEqpID;
                InData.INDATA[0].USERID = USERID.EIF;
                InData.INDATA[0].MDLLOT_ID = strModelID;
                #endregion


                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, null, out BizRuleErr);                
                iRet = BizCall(strBizName, strEqpID, InData, null, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;

                switch (iRet)
                {
                    case 0:
                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}", strBizName, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;
                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        //SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_FORM_CHK_PALLET_OUT
        protected int bizBrFormChkPalletOut(bool bTestMode, string strEqpID, string txnID, string strCstID, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;
            CVariable OutPara = null;

            try
            {
                string strBizName = "BR_FORM_CHK_PALLET_OUT";

                EIFLog(Level.Verbose, $"[{strBizName}] Start", strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_FORM_CHK_PALLET_OUT_IN InData = CBR_FORM_CHK_PALLET_OUT_IN.GetNew(this);
                CBR_FORM_CHK_PALLET_OUT_OUT OutData = CBR_FORM_CHK_PALLET_OUT_OUT.GetNew(this);

                #region Indata                
                InData.INDATA_LENGTH = 1;
                InData.INDATA[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.INDATA[0].IFMODE = bTestMode ? IFMODE.TestMode : IFMODE.OnLine;
                InData.INDATA[0].USERID = USERID.EIF;
                InData.INDATA[0].CSTID = strCstID;

                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, OutData.Variable, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, OutData, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;
                OutPara = OutData.Variable;

                switch (iRet)
                {
                    case 0:
                        EIFLog(Level.Verbose, $"[{strBizName}] Success \n{InPara.ToString()}{OutPara.ToString()}", strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;

                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, $"[{strBizName}] Fail - {BizRuleErr.Message} \n{InPara.ToString()}", strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion

        #region BR_PRD_GET_PALLET_INFO_BY_CSTID (물류)
        protected int bizBrPrdGetPalletInfoByCstId(bool bTestMode, string strEqpID, string txnID, string strLineID, string strModelID, List<CInPallet> lstPallet, out List<COutPallet> lstOutPallet, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;
            CVariable OutPara = null;

            lstOutPallet = new List<COutPallet>();
            COutPallet cOutPlt = null;

            try
            {
                string strBizName = "BR_PRD_GET_PALLET_INFO_BY_CSTID";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_PRD_GET_PALLET_INFO_BY_CSTID_IN InData = CBR_PRD_GET_PALLET_INFO_BY_CSTID_IN.GetNew(this);
                CBR_PRD_GET_PALLET_INFO_BY_CSTID_OUT OutData = CBR_PRD_GET_PALLET_INFO_BY_CSTID_OUT.GetNew(this);

                #region INDATA
                InData.INDATA_LENGTH = 1;
                InData.INDATA[0].CSTID = lstPallet[0].PALLETID;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, OutData.Variable, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, OutData, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;
                OutPara = OutData.Variable;

                switch (iRet)
                {
                    case 0:

                        for (int i = 0; i < OutData.OUTDATA_LENGTH; i++)
                        {
                            cOutPlt = new COutPallet();
                            cOutPlt.CSTID = OutData.OUTDATA[i].CSTID.Trim();
                            cOutPlt.HOST_PALLETID = OutData.OUTDATA[i].PALLETID.Trim();
                            cOutPlt.OUT_PALLET_TYPE = OutData.OUTDATA[i].CSTSTAT;
                            cOutPlt.EQPT_END_FLAG = OutData.OUTDATA[i].EQPT_END_FLAG;
                            lstOutPallet.Add(cOutPlt);
                        }

                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}{2}", strBizName, InPara.ToString(), OutPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;

                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }

        protected int bizBrPrdGetPalletInfoByCstId(bool bTestMode, string strEqpID, string txnID, string strLineID, string strModelID, string sPalletID, out List<COutPallet> lstOutPallet, out Exception BizRuleErr)
        {
            int iRet = -1;

            BizRuleErr = null;
            CVariable InPara = null;
            CVariable OutPara = null;

            lstOutPallet = new List<COutPallet>();
            COutPallet cOutPlt = null;

            try
            {
                string strBizName = "BR_PRD_GET_PALLET_INFO_BY_CSTID";

                EIFLog(Level.Verbose, string.Format("[{0}] Start", strBizName), strLogCategory, false, strEqpID, SHOPID.FORM);

                CBR_PRD_GET_PALLET_INFO_BY_CSTID_IN InData = CBR_PRD_GET_PALLET_INFO_BY_CSTID_IN.GetNew(this);
                CBR_PRD_GET_PALLET_INFO_BY_CSTID_OUT OutData = CBR_PRD_GET_PALLET_INFO_BY_CSTID_OUT.GetNew(this);

                #region INDATA
                InData.INDATA_LENGTH = 1;
                InData.INDATA[0].CSTID = sPalletID;
                #endregion

                //iRet = _EIFServer.FAService.Request(strBizName, InData.Variable, OutData.Variable, out BizRuleErr);
                iRet = BizCall(strBizName, strEqpID, InData, OutData, out BizRuleErr, txnID); //$ 2024.11.27 : Solace Biz 전환 //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가
                InPara = InData.Variable;
                OutPara = OutData.Variable;

                switch (iRet)
                {
                    case 0:

                        for (int i = 0; i < OutData.OUTDATA_LENGTH; i++)
                        {
                            cOutPlt = new COutPallet();
                            cOutPlt.CSTID = OutData.OUTDATA[i].CSTID.Trim();
                            cOutPlt.HOST_PALLETID = OutData.OUTDATA[i].PALLETID.Trim();
                            cOutPlt.OUT_PALLET_TYPE = OutData.OUTDATA[i].CSTSTAT;
                            cOutPlt.EQPT_END_FLAG = OutData.OUTDATA[i].EQPT_END_FLAG;
                            lstOutPallet.Add(cOutPlt);
                        }

                        EIFLog(Level.Verbose, string.Format("[{0}] Success \n{1}{2}", strBizName, InPara.ToString(), OutPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        break;

                    default:
                        RegBizRuleException(false, strEqpID, strBizName, string.Empty, InPara, BizRuleErr);
                        EIFLog(Level.Debug, string.Format("[{0}] Fail - {1} \n{2}", strBizName, BizRuleErr.Message, InPara.ToString()), strLogCategory, false, strEqpID, SHOPID.FORM);
                        SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                        break;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, strEqpID, SHOPID.FORM);
            }

            return iRet;
        }
        #endregion
        #endregion


        #region [ Load DB Mehtod]
        public void LoadCLCTITEM()
        {
            _dicCLCTITEM.Clear();

            DataSet ds = new DataSet();
            string strSQL = string.Empty;

            if (this.UNIT.Variables.ContainsKey("BASICINFO:V_EQP_UNIT_ID_01"))
            {
                strSQL = string.Format("SELECT * FROM TB_CLCTITEM_STND WHERE EQP_ID = '{0}' ORDER BY CLCTITEM_NO, CLCTTYPE", this.UNIT.Variables["BASICINFO:V_EQP_UNIT_ID_01"].AsString);
            }
            else
            {
                strSQL = string.Format("SELECT * FROM TB_CLCTITEM_STND WHERE EQP_ID = '{0}' ORDER BY CLCTITEM_NO, CLCTTYPE", this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString);
            }

            using (CDataManager mgr = new CDataManager())
            {
                mgr.GetDataSet(ds, "TB_CLCTITEM_STND", strSQL);
            }

            for (int i = 0; i < ds.Tables["TB_CLCTITEM_STND"].Rows.Count; i++)
            {
                CClctItem Item = new CClctItem(i);

                Item.EQPID = ds.Tables["TB_CLCTITEM_STND"].Rows[i]["EQP_ID"].ToString().Trim();
                Item.CLCTTYPE = ds.Tables["TB_CLCTITEM_STND"].Rows[i]["CLCTTYPE"].ToString().Trim();
                Item.CLCTITEMNO = int.Parse(ds.Tables["TB_CLCTITEM_STND"].Rows[i]["CLCTITEM_NO"].ToString());
                Item.CLCTITEM = ds.Tables["TB_CLCTITEM_STND"].Rows[i]["CLCTITEM"].ToString().Trim();
                Item.FPOINT = int.Parse(ds.Tables["TB_CLCTITEM_STND"].Rows[i]["FPOINT"].ToString());

                _dicCLCTITEM.Add(i, Item);
            }

            EIFLog(Level.Debug, $"[CEquipment] _dicCLCTITEM Count : {_dicCLCTITEM.Count}", strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString.Trim(), SHOPID.FORM);

            _strlstApdData = _dicCLCTITEM.Where(x => x.Value.CLCTTYPE == "APD_DATA").ToDictionary(x => x.Value.CLCTITEMNO, x => x.Value.CLCTITEM);
            _ilstApdDataFPoint = _dicCLCTITEM.Where(x => x.Value.CLCTTYPE == "APD_DATA").ToDictionary(x => x.Value.CLCTITEMNO, x => x.Value.FPOINT);
        }

        void LoadEioStateStnd()
        {
            _dicEioStateStnd.Clear();

            DataSet ds = new DataSet();

            string strSQL = string.Format("SELECT * FROM TB_EIOSTATE_STND ORDER BY EQPSTATE"); //JH 2024.11.14 mssql-> Oracle 인한 nolock 제거

            using (CDataManager mgr = new CDataManager())
            {
                mgr.GetDataSet(ds, "TB_EIOSTATE_STND", strSQL);
            }

            for (int i = 0; i < ds.Tables["TB_EIOSTATE_STND"].Rows.Count; i++)
            {
                CEioState State = new CEioState(i);

                State.EQPSTATE = ds.Tables["TB_EIOSTATE_STND"].Rows[i]["EQPSTATE"].ToString().Trim();
                State.EIOSTATE = ds.Tables["TB_EIOSTATE_STND"].Rows[i]["EIOSTATE"].ToString().Trim();
                State.ACTID = ds.Tables["TB_EIOSTATE_STND"].Rows[i]["ACTID"].ToString().Trim();
                State.EIONOTE = ds.Tables["TB_EIOSTATE_STND"].Rows[i]["EIONOTE"].ToString().Trim();

                _dicEioStateStnd.Add(i, State);
            }
        }

        #region Device Type Load DB
        protected void OnInstancingCompleted_DeviceType()
        {
            base.OnInstancingCompleted();

            strLogCategory = this.Name.Trim();

            string strEqpID = string.Empty;

            if (this.UNIT.Variables.ContainsKey("BASICINFO:V_DEVICE_TYPE"))
            {
                strEqpID = this.UNIT.Variables["BASICINFO:V_DEVICE_TYPE"].AsString;
            }
            else
                strEqpID = this.UNIT.Variables["BASICINFO:V_EQP_ID_01"].AsString;

            LoadCLCTITEM_DeviceType(strEqpID);

            LoadEioStateStnd();
        }

        void LoadCLCTITEM_DeviceType(string strEqpID)
        {
            DataSet ds = new DataSet();

            string strSQL = string.Format("SELECT * FROM TB_CLCTITEM_STND WHERE EQP_ID = '{0}' ORDER BY CLCTITEM_NO, CLCTTYPE", strEqpID); //JH 2024.11.14 mssql-> Oracle 인한 nolock 제거

            using (CDataManager mgr = new CDataManager())
            {
                mgr.GetDataSet(ds, "TB_CLCTITEM_STND", strSQL);
            }

            for (int i = 0; i < ds.Tables["TB_CLCTITEM_STND"].Rows.Count; i++)
            {
                CClctItem Item = new CClctItem(i);

                Item.EQPID = ds.Tables["TB_CLCTITEM_STND"].Rows[i]["EQP_ID"].ToString().Trim();
                Item.CLCTTYPE = ds.Tables["TB_CLCTITEM_STND"].Rows[i]["CLCTTYPE"].ToString().Trim();
                Item.CLCTITEMNO = int.Parse(ds.Tables["TB_CLCTITEM_STND"].Rows[i]["CLCTITEM_NO"].ToString()); //JH 2024.11.24 Oracle DB Table Datatype이 Number(Decimal)로 올라와 임시로 string -> int 로 변환 (Local Test 당시 테이블 속성 타입 정의가 INT가 안되었음)
                Item.CLCTITEM = ds.Tables["TB_CLCTITEM_STND"].Rows[i]["CLCTITEM"].ToString().Trim();
                Item.FPOINT = int.Parse(ds.Tables["TB_CLCTITEM_STND"].Rows[i]["FPOINT"].ToString()); //JH 2024.11.24 Oracle DB Table Datatype이 Number(Decimal)로 올라와 임시로 string -> int 로 변환 (Local Test 당시 테이블 속성 타입 정의가 INT가 안되었음)
                _dicCLCTITEM.Add(i, Item);
            }
        }
        #endregion
        #endregion


        #region [ Thread Method ]
        bool FirstTimeSync = true;
        protected void EquipmentCommunicationCheck()
        {
            try
            {
                //Wait(this.UNIT.BASICINFO__V_COM_CHECK_INTERVAL); //$ 2025.09.22 : Wait를 Scheduler 함수 내부에서 호출 시 해당 시간 만큼 무중단 패치 시 Delay걸려 주석 처리

                this.UNIT.HOST_COMM_CHK__O_B_HOST_COMM_CHK = TrigStatus.TRIG_OFF;

                Wait(COMMON_INTERVAL_SEC.SEC_1);
                this.UNIT.HOST_COMM_CHK__O_B_HOST_COMM_CHK = TrigStatus.TRIG_ON;

                DateTime dtNow = DateTime.Now;
                if (Convert.ToInt16(dtNow.Day) != iSendDateTimeday)
                {
                    bSendDateTime = false;
                    ApdLog_Delete();
                }

                //$ 2025.11.03 : 프로그램 기동 후 첫 진입 시 무조건 시간 동기화 하도록 로직 변경
                if (this.FirstTimeSync || ((dtNow.Hour.ToString() == (this.UNIT.BASICINFO__V_TIME_DATA_SEND_TIME)) && (bSendDateTime == false)))
                {
                    this.FirstTimeSync = false;
                    SendDateTimeData(IFMODE.OnLine, strEqpID, this.UNIT.Variables["BASICINFO:V_SYSTEM_DATA_TIME_SET_LOCAL_REMOTE"].AsBoolean, "DATE_TIME_SET_REQ:O_B_DATE_TIME_SET_REQ", "DATE_TIME_SET_REQ:O_W_DATE_TIME");
                    iSendDateTimeday = Convert.ToInt16(DateTime.Now.Day);
                    bSendDateTime = true;
                }
                else
                {
                    if (this.UNIT.DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ)
                    {
                        this.UNIT.DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ = false;
                    }
                }

                ConfirmBitForcedOff();
                ITBypassModeOff();
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        private void ConfirmBitForcedOff()
        {
            try
            {
                if (listReqeustVariable.Count == 0)
                    return;

                for (int i = 0; i < listReqeustVariable.Count; i++)
                {
                    if (listReqeustVariable[i].AsBoolean == false && listConfirmVariable[i].AsBoolean)
                    {
                        listConfirmVariable[i].AsBoolean = false;
                        EIFLog(Level.Debug, $"[{listConfirmVariable[i].Category.Name}] Confirm Bit Forced Off", strLogCategory, false, strEqpID, SHOPID.FORM);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        protected void DoModelIDReport()
        {
            try
            {
                //Wait(this.UNIT.BASICINFO__V_WIP_DATA_RPT_INTERVAL); //$ 2025.09.22 : Wait를 Scheduler 함수 내부에서 호출 시 해당 시간 만큼 무중단 패치 시 Delay걸려 주석 처리

                EIFLog(Level.Verbose, "Cycle Model ID Report : On", strLogCategory, false, strEqpID, SHOPID.FORM);

                Exception BizRuleErr = null;

                string strModelID = this.UNIT.S5_1_PROC_PARA_CHG_RPT_01__I_W_MODEL_ID.Trim();
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID;

                if (!string.IsNullOrWhiteSpace(strSubModelID))
                    bizBrFormRegModelForEOL(false, strEqpID, "", strSubModelID, out BizRuleErr);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        private void WipRWTDataReport()
        {
            try
            {
                //Wait(this.UNIT.BASICINFO__V_WIP_DATA_RPT_INTERVAL); //$ 2025.09.22 : Wait를 Scheduler 함수 내부에서 호출 시 해당 시간 만큼 무중단 패치 시 Delay걸려 주석 처리

                EIFLog(Level.Verbose, "Equipment Time Data Report : On", strLogCategory, false, strEqpID, SHOPID.FORM);

                //Func<List<ushort>, double> func = (x) => x[0] * 60 * 60 + x[1] * 60 + x[2]; 

                List<double> dblTimeData = new List<double>();
                string strDayNight = "0";

                //JH 2024.04.24 김인기 팀장님요청 : 설비 Run 상태를 제외한 나머지 상태에는 Tact Time 0 보고해야함 
                double tackTime = (this.UNIT.Variables["WIP_DATA_RPT:I_W_TACT_TIME"].AsShort / Math.Pow(10, double.Parse(this.UNIT.Variables["WIP_DATA_RPT:I_W_TACT_TIME"].ConnectionInfo["FPOINT", "0"])));
                tackTime = (this.UNIT.EQP_STAT_CHG_RPT__I_W_EQP_STAT == EQPSTATUS.RUN) ? tackTime : 0;

                dblTimeData.Add(0);
                dblTimeData.Add(0);
                dblTimeData.Add(0);
                dblTimeData.Add(0);
                dblTimeData.Add(tackTime);
                dblTimeData.Add(0);

                //$ 2022.10.04 : Tact Time 보고 시 Degas/EOL은 CellID가 필요하다고 함, Packer는 나오는데가 2 곳이기 때문에 1Lane을 검색하고 CellID가 없으면 2Lane을 검색하게 함
                string strCellID = this.UNIT.Variables["G4_1_CELL_ID_CONF_REQ_01:I_W_CELL_ID"].AsString.Trim();
                if (string.IsNullOrEmpty(strCellID))
                    strCellID = this.UNIT.Variables["G4_1_CELL_ID_CONF_REQ_02:I_W_CELL_ID"].AsString.Trim();

                if (!string.IsNullOrWhiteSpace(strMachineID))
                {
                    RegEqptOperInfo(false, strMachineID, strDayNight, dblTimeData, strCellID);

                    if (this.BASE.Variables["ADDINFO:V_MAIN_EQP_ID"].AsString == strMachineID)
                    {
                        RegEqptOperInfo(false, strEqpID, strDayNight, dblTimeData, strCellID);
                    }
                }
                else
                {
                    EIFLog(Level.Debug, "EQUIPMENT NAME IS NULL", strLogCategory, false, strEqpID, SHOPID.FORM);
                }

                EIFMonitoringData();
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }
        #endregion


        #region [ ETC Method ]

        #region ----------GMES Eqp Modification---------------------------------

        // 20180115 Host Alarm Function Change

        public void SendHostAlarmMsg(Exception BizRuleErr, string LangID, ushort uDisplayType, int iInterval = -1, int iDuration = -1, string strAlarmMode = null)
        {
            try
            {
                string strHostAlarmMsg = string.Empty;

                #region Host Alarm Message Interval/Duration 설정
                //Interval/Duration값을 조정할 수 있도록 함.
                if (iInterval == -1) iInterval = SCANINTERVAL;
                if (iDuration == -1) iDuration = NSECINTERVAL;
                #endregion

                if (BizRuleErr.Data["TYPE"] == null)
                {
                    EIFLog(Level.Debug, string.Format("{0}", "[ Exception Type is null ] " + BizRuleErr.Message.ToString()), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);

                    #region System Error HostAlarm 설정
                    if (!string.IsNullOrEmpty(strAlarmMode) && BizRuleErr.Data["CODE"] == null)
                    {
                        strHostAlarmMsg = "HOST(GMES) doesn't respond in timeout period";

                        int iHostAlarmLineCount = this.UNIT.Variables["ADDINFO:V_HOSTALARM_LINE_CNT"].AsInteger;
                        int iHostAlarmCharCnt = this.UNIT.Variables["ADDINFO:V_HOSTALARM_CHAR_CNT"].AsInteger;
                        int iHostAlarmByteSize = this.UNIT.Variables["ADDINFO:V_HOSTALARM_BYTE_SIZE"].AsInteger;

                        if (iHostAlarmLineCount > 0)
                        {
                            List<string> lstHostAlarmMsg = DividedStringLineByte(strHostAlarmMsg, iHostAlarmByteSize, LangID);

                            for (int i = 0; i < iHostAlarmLineCount; ++i)
                            {
                                if (i < lstHostAlarmMsg.Count)
                                {
                                    if (string.IsNullOrWhiteSpace(lstHostAlarmMsg[i].ToString()))
                                    {
                                        this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                                    }
                                    else
                                    {
                                        this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = lstHostAlarmMsg[i].ToString();
                                    }
                                }
                                else
                                {
                                    this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                                }
                            }
                        }
                        else
                        {
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].AsString = strHostAlarmMsg;
                        }

                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_DISP_TYPE"].AsShort = uDisplayType;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_EQP_PROC_STOP_TYPE"].AsShort = 0;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_ACTION"].AsShort = 0;

                        CVariableAction.NSecTrigger(this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND"], iInterval, iDuration);

                        EIFLog(Level.Verbose, string.Format("[EIF] {0} : {1}", "SendHostAlarmMsg", strHostAlarmMsg), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
                    }
                    else
                    {
                        //$ 2023.08.29 : Biz Server 강제 종료 등으로 Biz Error 발생시 Biz Exception등이 없는 경우로 설비로 Inform 없이 종료되어 하기 내역 추가
                        strHostAlarmMsg = "Default GMES Error. Please, Contact IT";

                        int iHostAlarmLineCount = this.UNIT.Variables["ADDINFO:V_HOSTALARM_LINE_CNT"].AsInteger;
                        int iHostAlarmCharCnt = this.UNIT.Variables["ADDINFO:V_HOSTALARM_CHAR_CNT"].AsInteger;
                        int iHostAlarmByteSize = this.UNIT.Variables["ADDINFO:V_HOSTALARM_BYTE_SIZE"].AsInteger;

                        if (iHostAlarmLineCount > 0)
                        {
                            List<string> lstHostAlarmMsg = DividedStringLineByte(strHostAlarmMsg, iHostAlarmByteSize, LangID);

                            for (int i = 0; i < iHostAlarmLineCount; ++i)
                            {
                                if (i < lstHostAlarmMsg.Count)
                                {
                                    if (string.IsNullOrWhiteSpace(lstHostAlarmMsg[i].ToString()))
                                    {
                                        this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                                    }
                                    else
                                    {
                                        this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = lstHostAlarmMsg[i].ToString();
                                    }
                                }
                                else
                                {
                                    this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                                }
                            }
                        }
                        else
                        {
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].AsString = strHostAlarmMsg;
                        }

                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_DISP_TYPE"].AsShort = uDisplayType;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_EQP_PROC_STOP_TYPE"].AsShort = 0;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_ACTION"].AsShort = 0;

                        CVariableAction.NSecTrigger(this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND"], iInterval, iDuration);

                        EIFLog(Level.Verbose, string.Format("[EIF] {0} : {1}", "SendHostAlarmMsg", strHostAlarmMsg), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
                    }
                    #endregion
                    return;
                }

                if (BizRuleErr.Data["TYPE"].ToString().Equals("USER"))
                {
                    string[] strarrErrPara = BizRuleErr.Data["PARA"].ToString().Split(':');

                    for (int i = 0; i < strarrErrPara.Length; i++)
                    {
                        strarrErrPara[i] = strarrErrPara[i].Trim();
                    }

                    //$ 2023.05.24 : 기존 Biz Error Code 전체를 DownLoad하여 메모리에 저장하고 이를 비교하여 언어 변환하던 내역을 기존 활성화 프로그램 처럼 Biz 호출하여 영문 전환하게 수정
                    string sMessage = BizRuleErr.Message.ToString().Trim();
                    string sTroubleCd = BizRuleErr.Data["DATA"].ToString().Trim();
                    string EqptID = this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString;

                    strHostAlarmMsg = sMessage;
                    if (Regex.IsMatch(sMessage, @"[ㄱ-ㅎ|ㅏ-ㅣ|가-힣%]")) //한글이나 %가 있는 경우 영문 변환 Biz를 호출
                    {
                        CBR_COM_GET_ERRORMESSAGE_IN InData = CBR_COM_GET_ERRORMESSAGE_IN.GetNew(this);
                        CBR_COM_GET_ERRORMESSAGE_OUT OutData = CBR_COM_GET_ERRORMESSAGE_OUT.GetNew(this);

                        sTroubleCd = sTroubleCd.TrimStart('0'); //$ 2023.10.11 : Host Biz Alarm은 090140로 오는데.. 이걸 다시 Biz로 영문 전환하면 없는 메시지 ID로 나와서 앞에 0은 제거

                        InData.INDATA_LENGTH = 1;

                        InData.INDATA[InData.INDATA_LENGTH - 1].MSGID = sTroubleCd;
                        InData.INDATA[InData.INDATA_LENGTH - 1].LANGID = LangID;

                        //int iRet = _EIFServer.FAService.Request("BR_COM_GET_ERRORMESSAGE", inData.Variable, outData.Variable, out BizRuleErr, false);
                        int iRet = BizCall("BR_COM_GET_ERRORMESSAGE", string.Empty, InData, OutData, out BizRuleErr, string.Empty, false); //$ 2024.11.27 : Solace Biz 전환

                        if (iRet != 0)
                        {
                            EIFLog(Level.Debug, string.Format("{0} : Fail", "BR_COM_GET_ERRORMESSAGE"), strLogCategory, false, EqptID, SHOPID.FORM);

                            RegBizRuleException(false, EqptID, "BR_COM_GET_ERRORMESSAGE", string.Empty, InData.Variable, BizRuleErr);
                        }

                        strHostAlarmMsg = OutData.OUTDATA[0].MSGNAME;

                        //Alarm Message에 %가 있다면 %숫자값 대신 정상적인 값으로 변경, %가 없다면 한글 유무 체크해서 한글은 제외하고 Message 생성
                        if (Regex.IsMatch(strHostAlarmMsg, @"[%]"))
                        {
                            for (int i = 0; i < strarrErrPara.Length; i++)
                                strHostAlarmMsg = strHostAlarmMsg.Replace("%" + (i + 1).ToString(), "{" + i.ToString() + "}");
                        }

                        //$ 2023.06.27 : Packer는 영문으로 트러블 명을 요청해도 한글이 포함되어 설비를 wString으로 변경
                        //$ 2023.10.16 : 영문으로 전환을 한 이후에도 한글이 남아 있을 경우 한글 삭제 시켜야 하지만, Mitsubishi PLC 다국어 설정 및 HostAlarm Address에 대한 Encoding 설정으로 처리함
                        //    if (Regex.IsMatch(strHostAlarmMsg, @"[ㄱ-ㅎ|ㅏ-ㅣ|가-힣]"))
                        //        strHostAlarmMsg = Regex.Replace(strHostAlarmMsg, @"[ㄱ-ㅎ|ㅏ-ㅣ|가-힣]", "");
                    }

                    strHostAlarmMsg = sTroubleCd + ":" + string.Format(strHostAlarmMsg, strarrErrPara);

                    int iHostAlarmLineCount = this.UNIT.Variables["ADDINFO:V_HOSTALARM_LINE_CNT"].AsInteger;

                    int iHostAlarmCharCnt = this.UNIT.Variables["ADDINFO:V_HOSTALARM_CHAR_CNT"].AsInteger;
                    int iHostAlarmByteSize = this.UNIT.Variables["ADDINFO:V_HOSTALARM_BYTE_SIZE"].AsInteger;

                    if (iHostAlarmLineCount > 0)
                    {
                        List<string> lstHostAlarmMsg = DividedStringLineByte(strHostAlarmMsg, iHostAlarmByteSize, LangID);

                        for (int i = 0; i < iHostAlarmLineCount; ++i)
                        {
                            if (i < lstHostAlarmMsg.Count)
                            {
                                if (string.IsNullOrWhiteSpace(lstHostAlarmMsg[i].ToString()))
                                {
                                    this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                                }
                                else
                                {
                                    this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = lstHostAlarmMsg[i].ToString();
                                }
                            }
                            else
                            {
                                this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                            }
                        }

                    }
                    else
                    {
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].AsString = strHostAlarmMsg;
                    }

                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_DISP_TYPE"].AsShort = uDisplayType;
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_EQP_PROC_STOP_TYPE"].AsShort = 0;
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_ACTION"].AsShort = 0;

                    CVariableAction.NSecTrigger(this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND"], iInterval, iDuration);

                    EIFLog(Level.Verbose, string.Format("{0} : {1}", "SendHostAlarmMsg", strHostAlarmMsg), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
                }
                else
                {
                    EIFLog(Level.Verbose, string.Format("{0}", "Exception Type is not USER"), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
        }

        //$ 2025.06.09 : 외부 시스템에 의한 Host Alarm 발생 Method 추가
        public void SendHostAlarmMsg(Exception BizRuleErr, string LangID, ushort uDisplayType, ushort uStopType, ushort uActiontype, int iInterval = -1, int iDuration = -1, string strHostAlarmMsg = "")
        {
            try
            {
                lock (_lockHostAlm)
                {
                    #region Host Alarm Message Interval/Duration 설정
                    //Interval/Duration값을 조정할 수 있도록 함.
                    if (iInterval == -1) iInterval = SCANINTERVAL;
                    if (iDuration == -1) iDuration = NSECINTERVAL;
                    #endregion

                    int iHostAlarmLineCount = this.UNIT.Variables["ADDINFO:V_HOSTALARM_LINE_CNT"].AsInteger;
                    //int iHostAlarmCharCnt = this.UNIT.Variables["ADDINFO:V_HOSTALARM_CHAR_CNT"].AsInteger;
                    int iHostAlarmByteSize = this.UNIT.Variables["ADDINFO:V_HOSTALARM_BYTE_SIZE"].AsInteger;

                    if (BizRuleErr.Data["CODE"] == null)
                    {
                        if (string.IsNullOrEmpty(strHostAlarmMsg) == true)
                        {
                            //$ 2023.08.29 : Biz Server 강제 종료 등으로 Biz Error 발생시 Biz Exception등이 없는 경우로 설비로 Inform 없이 종료되어 하기 내역 추가
                            strHostAlarmMsg = "Default Error. Please, Contact IT";
                        }
                        else
                        {
                            EIFLog(Level.Verbose, string.Format("{0}", "[ Trouble Code is null ] " + BizRuleErr.Message.ToString()), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.ASSY);
                        }
                    }
                    else
                    {
                        string strData = string.Empty;

                        strData = BizRuleErr.Data.Contains("DATA") ? BizRuleErr.Data["DATA"].ToString() : string.Empty;

                        string[] strarrErrPara = Regex.Split(strData, @"\|\|");

                        for (int i = 0; i < strarrErrPara.Length; i++)
                        {
                            strarrErrPara[i] = strarrErrPara[i].Trim();
                        }

                        //$ 2023.05.24 : 기존 Biz Error Code 전체를 DownLoad하여 메모리에 저장하고 이를 비교하여 언어 변환하던 내역을 기존 활성화 프로그램 처럼 Biz 호출하여 영문 전환하게 수정
                        string sMessage = BizRuleErr.Message.ToString().Trim();
                        //2025.03.26 | tlsrmsdl1 | Trouble Code 로 변경 - DATA 로 던지면 INDTA가 들어감
                        string sTroubleCd = BizRuleErr.Data["CODE"].ToString().Trim();
                        //string sTroubleCd = BizRuleErr.Data["DATA"].ToString().Trim();
                        string EqptID = this.Parent.Variables["BASICINFO:V_EQP_ID_01"].AsString;

                        strHostAlarmMsg = sMessage;

                        if (Regex.IsMatch(sMessage, @"[ㄱ-ㅎ|ㅏ-ㅣ|가-힣%]")) //한글이나 %가 있는 경우 영문 변환 Biz를 호출
                        {
                            CBR_COM_GET_ERRORMESSAGE_IN InData = CBR_COM_GET_ERRORMESSAGE_IN.GetNew(this);
                            CBR_COM_GET_ERRORMESSAGE_OUT OutData = CBR_COM_GET_ERRORMESSAGE_OUT.GetNew(this);

                            sTroubleCd = sTroubleCd.TrimStart('0'); //$ 2023.10.11 : Host Biz Alarm은 090140로 오는데.. 이걸 다시 Biz로 영문 전환하면 없는 메시지 ID로 나와서 앞에 0은 제거

                            InData.INDATA_LENGTH = 1;

                            InData.INDATA[InData.INDATA_LENGTH - 1].MSGID = sTroubleCd;
                            InData.INDATA[InData.INDATA_LENGTH - 1].LANGID = LangID;

                            //int iRet = _EIFServer.FAService.Request("BR_COM_GET_ERRORMESSAGE", inData.Variable, outData.Variable, out BizRuleErr, false);
                            int iRet = BizCall("BR_COM_GET_ERRORMESSAGE", string.Empty, InData, OutData, out BizRuleErr, string.Empty, false); //$ 2024.11.27 : Solace Biz 전환

                            if (iRet != 0)
                            {
                                EIFLog(Level.Debug, string.Format("{0} : Fail", "BR_COM_GET_ERRORMESSAGE"), strLogCategory, false, EqptID, SHOPID.ASSY);

                                RegBizRuleException(false, EqptID, "BR_COM_GET_ERRORMESSAGE", string.Empty, InData.Variable, BizRuleErr);
                            }

                            strHostAlarmMsg = OutData.OUTDATA[0].MSGNAME;

                            //Alarm Message에 %가 있다면 %숫자값 대신 정상적인 값으로 변경, %가 없다면 한글 유무 체크해서 한글은 제외하고 Message 생성
                            if (Regex.IsMatch(strHostAlarmMsg, @"[%]"))
                            {
                                for (int i = 0; i < strarrErrPara.Length; i++)
                                    strHostAlarmMsg = strHostAlarmMsg.Replace("%" + (i + 1).ToString(), "{" + i.ToString() + "}");
                            }

                            //$ 2023.06.27 : Packer는 영문으로 트러블 명을 요청해도 한글이 포함되어 설비를 wString으로 변경
                            //$ 2023.10.16 : 영문으로 전환을 한 이후에도 한글이 남아 있을 경우 한글 삭제 시켜야 하지만, Mitsubishi PLC 다국어 설정 및 HostAlarm Address에 대한 Encoding 설정으로 처리함
                            //    if (Regex.IsMatch(strHostAlarmMsg, @"[ㄱ-ㅎ|ㅏ-ㅣ|가-힣]"))
                            //        strHostAlarmMsg = Regex.Replace(strHostAlarmMsg, @"[ㄱ-ㅎ|ㅏ-ㅣ|가-힣]", "");
                        }

                        strHostAlarmMsg = sTroubleCd + ":" + string.Format(strHostAlarmMsg, strarrErrPara);
                    }

                    if (iHostAlarmLineCount > 0)
                    {
                        List<string> lstHostAlarmMsg = DividedStringLineByte(strHostAlarmMsg, iHostAlarmByteSize, LangID);

                        for (int i = 0; i < iHostAlarmLineCount; ++i)
                        {
                            if (i < lstHostAlarmMsg.Count)
                            {
                                if (string.IsNullOrWhiteSpace(lstHostAlarmMsg[i].ToString()))
                                {
                                    this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                                }
                                else
                                {
                                    this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = lstHostAlarmMsg[i].ToString();
                                }
                            }
                            else
                            {
                                this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                            }
                        }

                    }
                    else
                    {
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].AsString = strHostAlarmMsg;
                    }

                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_DISP_TYPE"].AsShort = uDisplayType;
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_EQP_PROC_STOP_TYPE"].AsShort = uStopType;
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_ACTION"].AsShort = uActiontype;

                    CVariableAction.NSecTrigger(this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND"], iInterval, iDuration);

                    EIFLog(Level.Verbose, string.Format("{0} : {1}", "SendHostAlarmMsg", strHostAlarmMsg), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.ASSY);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.ASSY);
            }
        }

        // Send EIF Message to PLC
        public void SendHostAlarmMsg(string EIFMsg, ushort uDisplayType, int iInterval = -1, int iDuration = -1)
        {
            try
            {
                int iHostAlarmLineCount = this.UNIT.Variables["ADDINFO:V_HOSTALARM_LINE_CNT"].AsInteger;

                int iHostAlarmCharCnt = this.UNIT.Variables["ADDINFO:V_HOSTALARM_CHAR_CNT"].AsInteger;
                int iHostAlarmByteSize = this.UNIT.Variables["ADDINFO:V_HOSTALARM_BYTE_SIZE"].AsInteger;

                #region Host Alarm Message Interval/Duration 설정
                //Interval/Duration값을 조정할 수 있도록 함.
                if (iInterval == -1) iInterval = SCANINTERVAL;
                if (iDuration == -1) iDuration = NSECINTERVAL;
                #endregion

                if (iHostAlarmLineCount > 0)
                {
                    List<string> lstHostAlarmMsg = DividedStringLineByte(EIFMsg, iHostAlarmByteSize, GLOBAL_LANGUAGE_SET.ENGLISH);

                    for (int i = 0; i < iHostAlarmLineCount; ++i)
                    {
                        if (i < lstHostAlarmMsg.Count)
                        {
                            if (string.IsNullOrWhiteSpace(lstHostAlarmMsg[i].ToString()))
                            {
                                this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                            }
                            else
                            {
                                this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = lstHostAlarmMsg[i].ToString();
                            }
                        }
                        else
                        {
                            this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                        }
                    }
                }
                else
                {
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].AsString = EIFMsg;
                }

                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_DISP_TYPE"].AsShort = uDisplayType;
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_EQP_PROC_STOP_TYPE"].AsShort = 0;
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_ACTION"].AsShort = 0;

                CVariableAction.NSecTrigger(this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND"], iInterval, iDuration);

                EIFLog(Level.Verbose, string.Format("{0} : {1}", "SendEIFAlarmMsg", EIFMsg), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
        }

        public void SendHostAlarmMsg(string strEIFMsg, ushort uDisplayType, string strLangType)
        {
            try
            {
                int iHostAlarmLineCount = this.UNIT.Variables["ADDINFO:V_HOSTALARM_LINE_CNT"].AsInteger;
                int iHostAlarmByteSize = this.UNIT.Variables["ADDINFO:V_HOSTALARM_BYTE_SIZE"].AsInteger;

                if (iHostAlarmLineCount > 0)
                {
                    List<string> lstHostAlarmMsg = DividedStringLineByte(strEIFMsg, iHostAlarmByteSize, strLangType);

                    for (int i = 0; i < iHostAlarmLineCount; ++i)
                    {
                        if (i < lstHostAlarmMsg.Count)
                        {
                            if (string.IsNullOrWhiteSpace(lstHostAlarmMsg[i].ToString()))
                            {
                                this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                            }
                            else
                            {
                                this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = lstHostAlarmMsg[i].ToString();
                            }
                        }
                        else
                        {
                            this.UNIT.Variables[string.Format("HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_{0:D2}", i + 1)].AsString = " ";
                        }
                    }
                }
                else
                {
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].AsString = strEIFMsg;
                }

                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_DISP_TYPE"].AsShort = uDisplayType;
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_EQP_PROC_STOP_TYPE"].AsShort = 0;
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_ACTION"].AsShort = 0;


                CVariableAction.NSecTrigger(this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND"], SCANINTERVAL, NSECINTERVAL);

                EIFLog(Level.Verbose, string.Format("{0} : {1}", "SendEIFAlarmMsg", strEIFMsg), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
        }

        private static List<string> DividedStringLine(string strHostMsg, int count)
        {
            List<string> lstString = new List<string>();

            string[] strlist = strHostMsg.Split(' ');

            string str = string.Empty;

            for (int i = 0; i < strlist.Length; ++i)
            {
                if (strlist[i].Length > count)
                {
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        lstString.Add(str);
                    }
                    str = strlist[i].Substring(0, count);
                    lstString.Add(str);
                    str = string.Empty;

                    strlist[i] = strlist[i].Substring(count);
                    --i;
                    continue;
                }


                if (str.Length > (count - strlist[i].Length))
                {
                    lstString.Add(str);
                    str = string.Empty;
                }

                str += strlist[i] + ' ';
            }
            lstString.Add(str);


            return lstString;
        }

        private List<string> DividedStringLineByte(string strHostMsg, int iByteSize = 24, string iLangType = GLOBAL_LANGUAGE_SET.ENGLISH)
        {
            List<string> lstString = new List<string>();

            string sTemp = "";
            string sLong = "";

            string sMessage1 = "";
            string sLanguage = "";

            //int iTotalByte = 94;

            switch (iLangType)
            {
                case GLOBAL_LANGUAGE_SET.KOREA:
                    sLanguage = "euc-kr";
                    break;
                case GLOBAL_LANGUAGE_SET.ENGLISH:
                    sLanguage = "euc-kr";
                    break;
                case GLOBAL_LANGUAGE_SET.CHINA:
                    sLanguage = "gb2312";
                    break;
                case GLOBAL_LANGUAGE_SET.POLAND:
                    sLanguage = "cp852";
                    break;
                case GLOBAL_LANGUAGE_SET.UKRAINE:
                    sLanguage = "cp866";
                    break;
                case GLOBAL_LANGUAGE_SET.RUSSIA:
                    sLanguage = "cp866";
                    break;
                default:
                    sLanguage = "euc-kr";
                    break;
            }

            if (this.BASE.Variables["ADDINFO:V_HMI_EQP"].AsString.Equals("GOT2000")) sLanguage = "UTF-16";

            int iHostAlarmCharCnt = this.UNIT.Variables["ADDINFO:V_HOSTALARM_CHAR_CNT"].AsInteger;

            if (iHostAlarmCharCnt < 24) iHostAlarmCharCnt = 24; // 최소값 설정

            for (int i = 0; i <= strHostMsg.Length - 1; i++)
            {
                sLong = sLong + strHostMsg.Substring(i, 1);
                byte[] TotalByte = System.Text.Encoding.GetEncoding(sLanguage).GetBytes(sLong);

                //if (TotalByte.Length <= iTotalByte)  
                //{
                sTemp = sTemp + strHostMsg.Substring(i, 1);
                byte[] ChinaByte = System.Text.Encoding.GetEncoding(sLanguage).GetBytes(sTemp);

                if (ChinaByte.Length > iByteSize || iHostAlarmCharCnt <= sTemp.Length)
                {
                    sMessage1 = sTemp.Substring(0, sTemp.Length - 1);
                    lstString.Add(sMessage1);
                    sTemp = "";
                    i--;
                }

                if (sLong == strHostMsg && ChinaByte.Length < iByteSize)
                {
                    sMessage1 = sTemp.Substring(0, sTemp.Length);
                    lstString.Add(sMessage1);
                    sTemp = "";
                }
            }

            if (!string.IsNullOrEmpty(sTemp))
            {
                lstString.Add(sTemp);
                sTemp = "";
            }

            if (this.UNIT.Variables["ADDINFO:V_HOSTALARM_LINE_CNT"].AsInteger > lstString.Count)
            {
                int iLoopCnt = this.UNIT.Variables["ADDINFO:V_HOSTALARM_LINE_CNT"].AsInteger - lstString.Count;
                for (int i = 1; i <= iLoopCnt; i++)
                {
                    lstString.Add("");
                }
            }

            return lstString;
        }

        // 20180115 Host Alarm Function Change
        public void GuiLanguageTypeChanged(ushort value)
        {
            int iHostAlarmLineCnt = this.UNIT.Variables["ADDINFO:V_HOSTALARM_LINE_CNT"].AsInteger;

            try
            {
                CDriver SendVariableDriver = ((CIOVariable)this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND"]).Driver;

                if (SendVariableDriver == null)
                {
                    EIFLog(Level.Debug, "[HOST_ALARM_MSG_SEND:O_B_HOST_ALARM_MSG_SEND] Driver not mapped", strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
                    return;
                }

                //TODO $ 2025.04.23 : Driver 등록하고 여기 진입하는지 확인 필요

                if (SendVariableDriver.GetType().Name.ToUpper().Contains("MXCOMPONENT"))
                {
                    if (this.BASE.Variables["ADDINFO:V_HMI_EQP"].AsString.Equals("GOT2000"))
                    {
                        SetHostAlarmMsgVarVarforMxComponent_GOT2000(value, iHostAlarmLineCnt);
                    }
                    else
                    {
                        SetHostAlarmMsgVarVarforMxComponent(value, iHostAlarmLineCnt);
                    }
                }
                else if (SendVariableDriver.GetType().Name.ToUpper().Contains("OMRON"))
                {
                    SetHostAlarmMsgVarVarforOmron(value, iHostAlarmLineCnt);
                }

                EIFLog(Level.Verbose, string.Format("{0} : {1}", "GuiLanguageType Changed", value + " [" + strLangID + "]"), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
        }

        void SetHostAlarmMsgVarVarforMxComponent(ushort GuiLangtype, int HostAlarmLineCnt)
        {
            if (HostAlarmLineCnt > 0)
            {
                for (int i = 0; i < HostAlarmLineCnt; i++)
                {
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Remove("ENCODING");
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Remove("BYTE_SWAP");

                    switch (GuiLangtype)
                    {
                        case GLOBAL_LANGUAGE.KOREA:
                            strLangID = GLOBAL_LANGUAGE_SET.KOREA;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "euc-kr");
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTE_SWAP", "1");
                            break;
                        case GLOBAL_LANGUAGE.ENGLISH:
                            strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTE_SWAP", "1");
                            break;
                        case GLOBAL_LANGUAGE.CHINA:
                            strLangID = GLOBAL_LANGUAGE_SET.CHINA;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "gb2312");
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTE_SWAP", "1");
                            break;
                        case GLOBAL_LANGUAGE.POLAND:
                            strLangID = GLOBAL_LANGUAGE_SET.POLAND;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "cp852");
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTE_SWAP", "1");
                            break;
                        case GLOBAL_LANGUAGE.UKRAINE:
                            strLangID = GLOBAL_LANGUAGE_SET.UKRAINE;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "cp866");
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTE_SWAP", "1");
                            break;
                        case GLOBAL_LANGUAGE.RUSSIA:
                            strLangID = GLOBAL_LANGUAGE_SET.RUSSIA;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "cp866");
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTE_SWAP", "1");
                            break;
                        default:
                            strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                            break;
                    }
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfoString = this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.ToString();
                }
            }
            else
            {
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Remove("ENCODING");
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Remove("BYTE_SWAP");

                switch (GuiLangtype)
                {
                    case GLOBAL_LANGUAGE.KOREA:
                        strLangID = GLOBAL_LANGUAGE_SET.KOREA;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "euc-kr");
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTE_SWAP", "1");
                        break;
                    case GLOBAL_LANGUAGE.ENGLISH:
                        strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTE_SWAP", "1");
                        break;
                    case GLOBAL_LANGUAGE.CHINA:
                        strLangID = GLOBAL_LANGUAGE_SET.CHINA;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "gb2312");
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTE_SWAP", "1");
                        break;
                    case GLOBAL_LANGUAGE.POLAND:
                        strLangID = GLOBAL_LANGUAGE_SET.POLAND;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "cp852");
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTE_SWAP", "1");
                        break;
                    case GLOBAL_LANGUAGE.UKRAINE:
                        strLangID = GLOBAL_LANGUAGE_SET.UKRAINE;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "cp866");
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTE_SWAP", "1");
                        break;
                    case GLOBAL_LANGUAGE.RUSSIA:
                        strLangID = GLOBAL_LANGUAGE_SET.RUSSIA;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "cp866");
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTE_SWAP", "1");
                        break;
                    default:
                        strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                        break;
                }
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfoString = this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.ToString();

            }
        }

        void SetHostAlarmMsgVarVarforOmron(ushort GuiLangtype, int HostAlarmLineCnt)
        {
            if (HostAlarmLineCnt > 0)
            {
                for (int i = 0; i < HostAlarmLineCnt; i++)
                {
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Remove("ENCODING");
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Remove("BYTESWAP");

                    switch (GuiLangtype)
                    {
                        case GLOBAL_LANGUAGE.KOREA:
                            strLangID = GLOBAL_LANGUAGE_SET.KOREA;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "euc-kr");
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTESWAP", "FALSE");
                            break;
                        case GLOBAL_LANGUAGE.ENGLISH:
                            strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTESWAP", "FALSE");
                            break;
                        case GLOBAL_LANGUAGE.CHINA:
                            strLangID = GLOBAL_LANGUAGE_SET.CHINA;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "gb2312");
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTESWAP", "FALSE");
                            break;
                        case GLOBAL_LANGUAGE.POLAND:
                            strLangID = GLOBAL_LANGUAGE_SET.POLAND;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "cp852");
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTESWAP", "FALSE");
                            break;
                        case GLOBAL_LANGUAGE.UKRAINE:
                            strLangID = GLOBAL_LANGUAGE_SET.UKRAINE;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "cp866");
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTESWAP", "FALSE");
                            break;
                        case GLOBAL_LANGUAGE.RUSSIA:
                            strLangID = GLOBAL_LANGUAGE_SET.RUSSIA;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "cp866");
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTESWAP", "FALSE");
                            break;
                        default:
                            strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                            this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("BYTESWAP", "FALSE");
                            break;
                    }
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfoString = this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.ToString();
                }
            }
            else
            {
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Remove("ENCODING");
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Remove("BYTE_SWAP");

                switch (GuiLangtype)
                {
                    case GLOBAL_LANGUAGE.KOREA:
                        strLangID = GLOBAL_LANGUAGE_SET.KOREA;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "euc-kr");
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTESWAP", "FALSE");
                        break;
                    case GLOBAL_LANGUAGE.ENGLISH:
                        strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTESWAP", "FALSE");
                        break;
                    case GLOBAL_LANGUAGE.CHINA:
                        strLangID = GLOBAL_LANGUAGE_SET.CHINA;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "gb2312");
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTESWAP", "FALSE");
                        break;
                    case GLOBAL_LANGUAGE.POLAND:
                        strLangID = GLOBAL_LANGUAGE_SET.POLAND;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "cp852");
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTESWAP", "FALSE");
                        break;
                    case GLOBAL_LANGUAGE.UKRAINE:
                        strLangID = GLOBAL_LANGUAGE_SET.UKRAINE;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "cp866");
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTESWAP", "FALSE");
                        break;
                    case GLOBAL_LANGUAGE.RUSSIA:
                        strLangID = GLOBAL_LANGUAGE_SET.RUSSIA;
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "cp866");
                        this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("BYTESWAP", "FALSE");
                        break;
                    default:
                        strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                        break;
                }
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfoString = this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.ToString();

            }
        }

        void SetHostAlarmMsgVarVarforMxComponent_GOT2000(ushort GuiLangtype, int HostAlarmLineCnt)
        {
            if (HostAlarmLineCnt > 0)
            {
                for (int i = 0; i < HostAlarmLineCnt; i++)
                {
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Remove("ENCODING");
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Remove("BYTE_SWAP");

                    switch (GuiLangtype)
                    {
                        case GLOBAL_LANGUAGE.KOREA:
                            strLangID = GLOBAL_LANGUAGE_SET.KOREA;
                            break;
                        case GLOBAL_LANGUAGE.ENGLISH:
                            strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                            break;
                        case GLOBAL_LANGUAGE.CHINA:
                            strLangID = GLOBAL_LANGUAGE_SET.CHINA;
                            break;
                        case GLOBAL_LANGUAGE.POLAND:
                            strLangID = GLOBAL_LANGUAGE_SET.POLAND;
                            break;
                        case GLOBAL_LANGUAGE.UKRAINE:
                            strLangID = GLOBAL_LANGUAGE_SET.UKRAINE;
                            break;
                        case GLOBAL_LANGUAGE.RUSSIA:
                            strLangID = GLOBAL_LANGUAGE_SET.RUSSIA;
                            break;
                        default:
                            strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                            break;
                    }
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.Add("ENCODING", "UTF-16");
                    this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfoString = this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG_" + (i + 1).ToString("00")].ConnectionInfo.ToString();
                }
            }
            else
            {
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Remove("ENCODING");
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Remove("BYTE_SWAP");

                switch (GuiLangtype)
                {
                    case GLOBAL_LANGUAGE.KOREA:
                        strLangID = GLOBAL_LANGUAGE_SET.KOREA;
                        break;
                    case GLOBAL_LANGUAGE.ENGLISH:
                        strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                        break;
                    case GLOBAL_LANGUAGE.CHINA:
                        strLangID = GLOBAL_LANGUAGE_SET.CHINA;
                        break;
                    case GLOBAL_LANGUAGE.POLAND:
                        strLangID = GLOBAL_LANGUAGE_SET.POLAND;
                        break;
                    case GLOBAL_LANGUAGE.UKRAINE:
                        strLangID = GLOBAL_LANGUAGE_SET.UKRAINE;
                        break;
                    case GLOBAL_LANGUAGE.RUSSIA:
                        strLangID = GLOBAL_LANGUAGE_SET.RUSSIA;
                        break;
                    default:
                        strLangID = GLOBAL_LANGUAGE_SET.ENGLISH;
                        break;
                }
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.Add("ENCODING", "UTF-16");
                this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfoString = this.UNIT.Variables["HOST_ALARM_MSG_SEND:O_W_HOST_ALARM_MSG"].ConnectionInfo.ToString();

            }
        }

        #endregion

        protected string GetClctItemValue(CVariable var, int APDFpoint)
        {
            string sRet = string.Empty;

            switch (var.DataType)
            {
                case enumDataType.String:
                    sRet = var.AsString.Trim();
                    break;
                case enumDataType.Integer:
                    sRet = (var.AsInteger / Math.Pow(10, APDFpoint)).ToString();
                    break;
                case enumDataType.Short:
                    sRet = (var.AsShort / Math.Pow(10, APDFpoint)).ToString();
                    break;
                case enumDataType.Boolean:
                    sRet = var.AsBoolean ? "OK" : "NG";
                    break;
            }

            return sRet;
        }

        public string GetEqpType(string IpKeyName)
        {
            StringBuilder result = new StringBuilder(255);
            string inifile = @"D:\EQPINFO.ini";

            try
            {
                GetPrivateProfileString("EQP_TYPE", IpKeyName, string.Empty, result, result.Capacity, inifile);
            }
            catch (Exception)
            { }

            return result.ToString();
        }

        public void RegEqptDataClct_APD(string EqptID, string txnID, string UnitID, string LotID, string SubLotID, int SeqNo, string EventName, List<string> strlstClctItem, Dictionary<string, List<string>> dicItemVal, string Judge, bool Logging)
        {
            int iRet = -1;

            Exception BizRuleErr = null;
            CVariable InPara = null;

            if (strlstClctItem.Count == 0)
            {
                EIFLog(Level.Debug, string.Format("{0} : CLCTITEM Count = 0", "BR_QCA_REG_EQPT_DATA_CLCT"), strLogCategory, false, EqptID, SHOPID.FORM);

                return;
            }

            EIFLog(Level.Verbose, string.Format("Eqpt Data Collect Save Start"), strLogCategory, false, EqptID, SHOPID.FORM);

            try
            {
                lock (objLockRegEqptDataClct)
                {
                    CBR_QCA_REG_EQPT_DATA_CLCT_IN InData = CBR_QCA_REG_EQPT_DATA_CLCT_IN.GetNew(this);

                    InData.IN_EQP_LENGTH = 1;

                    InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                    InData.IN_EQP[0].IFMODE = IFMODE.OnLine;
                    InData.IN_EQP[0].EQPTID = EqptID;
                    InData.IN_EQP[0].UNIT_EQPTID = UnitID;    //2017.02.02 Unit ID 
                    InData.IN_EQP[0].USERID = USERID.EIF;
                    InData.IN_EQP[0].LOTID = LotID;
                    InData.IN_EQP[0].SUBLOTID = SubLotID;
                    InData.IN_EQP[0].INPUT_SEQ_NO = SeqNo;
                    InData.IN_EQP[0].EVENT_NAME = EventName;


                    InData.IN_DATA_LENGTH = strlstClctItem.Count;
                    for (int i = 0; i < strlstClctItem.Count; i++)
                    {
                        InData.IN_DATA[i].CLCTITEM = strlstClctItem[i];

                        for (int j = 0; j < dicItemVal[strlstClctItem[i]].Count; j++)
                        {
                            InData.Variable.Structure[1].StructureList[i].Variables[j + 6].Value = dicItemVal[strlstClctItem[i]][j];
                        }
                    }

                    //iRet = _EIFServer.FAService.Request("BR_QCA_REG_EQPT_DATA_CLCT", InData.Variable, null, out BizRuleErr, Logging);
                    iRet = BizCall("BR_QCA_REG_EQPT_DATA_CLCT", EqptID, InData, null, out BizRuleErr, txnID, false); //$ 2025.05.21 : Solace Log 생성을 위한 txnID 추가

                    InPara = InData.Variable;

                    ApdLog(InPara);

                }
                if (iRet == 0)
                {
                    EIFLog(Level.Verbose, string.Format("{0} : Success", "BR_QCA_REG_EQPT_DATA_CLCT"), strLogCategory, false, EqptID, SHOPID.FORM);

                }
                else
                {
                    EIFLog(Level.Debug, string.Format("{0} : Fail", "BR_QCA_REG_EQPT_DATA_CLCT"), strLogCategory, false, EqptID, SHOPID.FORM);

                    RegBizRuleException(false, EqptID, "BR_QCA_REG_EQPT_DATA_CLCT", LotID, InPara, BizRuleErr);

                    SOLExLog(BizRuleErr.ToString(), strLogCategory, strEqpID, SHOPID.FORM, txnID);  //$ 2025.05.21 : Biz Error 발생에 대한 Log 추가
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, EqptID, SHOPID.FORM);
            }
        }

        public void ApdLog(CVariable InPara)
        {
            string log = string.Empty;

            for (int i = 0; i < InPara.Structure.Variables.Count; i++)
            {
                for (int j = 0; j < InPara.Structure.Variables[i].StructureList.Count; j++)
                {
                    if (j == 0)
                    {
                        for (int x = 0; x < InPara.Structure.Variables[i].StructureList[j].Variables.Count; x++)
                        {
                            log += "[" + InPara.Structure.Variables[i].StructureList[j].Variables[x].Name + "]\t";
                        }

                        log += "\r\n";
                    }

                    for (int x = 0; x < InPara.Structure.Variables[i].StructureList[j].Variables.Count; x++)
                    {
                        log += "[" + InPara.Structure.Variables[i].StructureList[j].Variables[x].Value + "]\t";
                    }

                    log += "\r\n";
                }
            }
            EIFLog(Level.Verbose, log, "APD_DATA", false, strEqpID, SHOPID.FORM);


        }

        public void ApdLog_Delete()
        {

            DirectoryInfo gtDir = new DirectoryInfo(_logPath);
            FileInfo[] gtFiles = gtDir.GetFiles();

            DateTime dt = DateTime.Now;

            for (int i = 0; i < gtFiles.Length; i++)
            {
                if (gtFiles[i].Name.Contains("APD_DATA"))
                {
                    try
                    {
                        string name = gtFiles[i].Name.Replace("APD_DATA_", "");

                        DateTime dt2 = DateTime.ParseExact(dt.Year.ToString() + name.Substring(0, 4), "yyyyMMdd", null);

                        if ((dt - dt2).Days > this.UNIT.Variables["BASICINFO:V_APD_STORAGE_PERIOD"].AsInteger)
                        {
                            gtFiles[i].Delete();
                        }
                    }
                    catch { }
                }

            }
        }

        private void SendRfidReadingResult(CVariable sender, string strPstnID, string strScanRst, string strInOutType, string strCarrierID)
        {
            CInScan CinScanData = new CInScan();

            bool bRFIDUsing = this.UNIT.Variables[$"{sender.Category.Name}:I_B_RFID_USING"].AsBoolean;
            ushort uReadingType = this.UNIT.Variables[$"{sender.Category.Name}:I_W_READING_TYPE"].AsShort;

            CinScanData.EQPT_MOUNT_PSTN_ID = strPstnID;
            CinScanData.SCAN_TYPE = GetIDReadingType(uReadingType);
            CinScanData.SCAN_RSLT = strScanRst;
            CinScanData.IN_OUT_TYPE = strInOutType;
            CinScanData.CSTID = strCarrierID;

            RegPrdRegEqptScanRst(bTestMode, strEqpID, CinScanData);
        }

        private string GetIDReadingType(ushort uReadingType)
        {
            string strReadingType = string.Empty;

            switch (uReadingType)
            {
                case 1: strReadingType = "F"; break;
                case 2: strReadingType = "B"; break;
                case 3: strReadingType = "H"; break;
                default: break;
            }
            return strReadingType;
        }

        ushort PalletInOutTypeToInt(string strCstStat)
        {
            ushort uPalletStatus;

            switch (strCstStat)
            {
                case PALLET_STATUS.PLT_EMPTY:
                    uPalletStatus = PALLET_OUTPUT_TYPE.PALLET_EMPTY;
                    break;
                case PALLET_STATUS.TRAY_EMPTY:
                    uPalletStatus = PALLET_OUTPUT_TYPE.TRAY_PALLET_EMPTY;
                    break;
                case PALLET_STATUS.PALLET_OK:
                    uPalletStatus = PALLET_OUTPUT_TYPE.PALLET_FULL;
                    break;
                case PALLET_STATUS.PALLET_NG:
                    uPalletStatus = PALLET_OUTPUT_TYPE.PALLET_NG;
                    break;
                default:
                    uPalletStatus = 0;
                    break;
            }

            return uPalletStatus;
        }

        //Cell NG Type 반환
        string CellNgType(ushort NgCode)
        {
            string strNGMsg = string.Empty;

            switch (NgCode)
            {
                case 1:
                    strNGMsg = "Cell ID Confirm Error";
                    break;
                case 2:
                    strNGMsg = "PLC 1D Barcode Read Error";
                    break;
                case 3:
                    strNGMsg = "PLC 2D Barcode Read Error";
                    break;
                case 4:
                    strNGMsg = "Vision Appearance Bad";
                    break;
                case 5:
                    strNGMsg = "Vision Size Bad";
                    break;
                case 6:
                    strNGMsg = "Load Fail";
                    break;
                case 7:
                    strNGMsg = "Time Out NG";
                    break;
                case 8:
                    strNGMsg = "Region Error";
                    break;
                case 9:
                    strNGMsg = "Dimension Error";
                    break;
                case 11:
                    strNGMsg = "Scrap Multi Surfi";
                    break;
                case 12:
                    strNGMsg = "Crack NG";
                    break;
                case 15:
                    strNGMsg = "Scrap Multi DPU";
                    break;
                case 16:
                    strNGMsg = "Scrap Multi BOTH";
                    break;
                default:
                    break;
            }

            return strNGMsg;
        }

        //$ 2023.02.02 : Alarm 다중 보고 대응을 위한 추가
        private void HostConfirmBitOff(CVariable sender, bool value)
        {
            if (value == true)
            {
                Task.Factory.StartNew(() =>
                {
                    if (CVariableAction.TimeOut(this.UNIT.Variables[sender.NameCategorized], false, SCANINTERVAL, TIMEOUTSEC))
                    {
                        this.UNIT.Variables[sender.NameCategorized].Value = false;
                        EIFLog(Level.Warning, $"[{sender.Name}] TimeOut ({TIMEOUTSEC}sec)", strLogCategory, false, strEqpID, SHOPID.FORM);
                    }

                });
            }
        }

        //$ 2024.02.29 : 테스트 모드시 OK 및 관련 데이터를 Update하는 Method
        public void HostTestModeOKReport(CVariable sender, string txnID, string strLogCat)
        {
            int x = 1;

            foreach (CVariable vari in this.UNIT.Variables.Values.Where((p) => p.Category.Name == sender.Category.Name && p.AccessType == enumAccessType.Out))
            {
                if (vari.DataType == enumDataType.Short)
                {
                    if (vari.Name.Equals(CTag.O_W_TRIGGER_REPORT_ACK))
                        vari.AsShort = ConfirmAck.OK;
                    else
                        vari.AsShort = (ushort)x;

                    SOLLog(Level.Info, $"[STEP_6] {sender.Category.Name + ":" + CTag.O_W_TRIGGER_REPORT_ACK} : {vari.AsShort}", strLogCategory, strLogCat, SHOPID.FORM, txnID);

                }
                else if (vari.DataType == enumDataType.Integer)
                {
                    //$ 2024.02.16 : Dry Run Mode의 경우 Host 영역의 데이터를 임의 생성하나 NG_TYPE에 임의의 값이 설정 될 경우 불량 배출 되므로 따로 조건 처리함
                    if (vari.Name.Contains("O_W_NG_TYPE"))
                        vari.AsInteger = 10;
                    else
                        vari.AsInteger = x * 100;

                    SOLLog(Level.Info, $"[STEP_6] {sender.Category.Name + ":" + CTag.O_W_TRIGGER_REPORT_ACK} : {vari.AsShort}", strLogCategory, strLogCat, SHOPID.FORM, txnID);
                }
                else if (vari.DataType == enumDataType.String) vari.AsString = vari.Name.Replace("_", string.Empty).Substring(2, 4) + DateTime.Now.ToString("HHmmss");
                else if (vari.DataType == enumDataType.Boolean)
                {
                    if (vari.Name.Equals(CTag.O_B_TRIGGER_REPORT_CONF))
                        continue;
                }
                x++;
            }
        }

        //$ 2024.02.29 : 테스트 모드시 NG 및 Host Alarm을 발생시키는 Method
        public void HostTestModeNGReport(CVariable sender, string txnID, string strLogCat)
        {
            if (this.UNIT.Variables.ContainsKey(sender.Category.Name + ":" + CTag.O_W_TRIGGER_REPORT_ACK))
            {
                this.UNIT.Variables[sender.Category.Name + ":" + CTag.O_W_TRIGGER_REPORT_ACK].AsShort = ConfirmAck.NG;

                SOLLog(Level.Warning, $"[STEP_6] {sender.Category.Name + ":" + CTag.O_W_TRIGGER_REPORT_ACK} : 11", strLogCategory, strLogCat, SHOPID.FORM, txnID);
            }

            string strNGMessage = string.Empty;

            switch (this.UNIT.EQP_OP_MODE_CHG_RPT__I_W_HMI_LANG_TYPE)
            {
                case GLOBAL_LANGUAGE.KOREA:
                    strNGMessage = "테스트 NG1                테스트 NG2                테스트 NG3                테스트 NG4  ";
                    break;
                case GLOBAL_LANGUAGE.ENGLISH:
                    strNGMessage = "TEST NG1                TEST NG2                TEST NG3                TEST NG4  ";
                    break;
                case GLOBAL_LANGUAGE.CHINA:
                    strNGMessage = "测试 NG1                测试 NG2                测试 NG3                测试 NG4  ";
                    break;
                default:
                    strNGMessage = "TEST NG1                TEST NG2                TEST NG3                TEST NG4  ";
                    break;
            }
            Task.Factory.StartNew(() =>
            {
                SendHostAlarmMsg(strNGMessage, HOST_ALM_TYPE.COMM_TYPE, strLangID);
            });
        }

        protected void SendDateTimeData(string TestMode, string EqptID, bool SetLocalSvr, string RequestVarName, string TimeDataVarName)
        {
            try
            {
                DateTime dtSvrTime = DateTime.Now;

                //2025.08.21 JMS : this.UNIT 추가 (Packer 모델링 구조로 인해, Variables만 있으면 UNIT을 참조 못해서 에러 발생)
                if (!this.UNIT.Variables["BASICINFO:V_SET_LOCAL_DATE_TIME"].AsBoolean)
                {
                    GetSystemTime(TestMode, EqptID, out dtSvrTime);
                }

                List<ushort> lstTimeData = new List<ushort>(6);
                lstTimeData.Add(Convert.ToUInt16(dtSvrTime.Year));
                lstTimeData.Add(Convert.ToUInt16(dtSvrTime.Month));
                lstTimeData.Add(Convert.ToUInt16(dtSvrTime.Day));
                lstTimeData.Add(Convert.ToUInt16(dtSvrTime.Hour));
                lstTimeData.Add(Convert.ToUInt16(dtSvrTime.Minute));
                lstTimeData.Add(Convert.ToUInt16(dtSvrTime.Second));

                SetTime(lstTimeData, SetLocalSvr, TimeDataVarName);

                EIFLog(Level.Verbose, string.Format("{0} : {1}", "SendDateTimeData", dtSvrTime.ToString()), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
            finally
            {
                CVariableAction.NSecTrigger(this.UNIT.Variables[RequestVarName], SCANINTERVAL, NSECINTERVAL);
            }
        }

        protected void SetTime(List<ushort> lstTimeData, bool SetLocalSvr, string TimeDataVarName)
        {
            try
            {
                DBTIME stData = new DBTIME();
                stData.wYear = lstTimeData[0];
                stData.wMonth = lstTimeData[1];
                stData.wDay = lstTimeData[2];
                stData.wHour = lstTimeData[3];
                stData.wMinute = lstTimeData[4];
                stData.wSecond = lstTimeData[5];

                if (SetLocalSvr)
                    SetLocalTime(ref stData);

                this.UNIT.Variables[TimeDataVarName].AsShortList = lstTimeData;

                EIFLog(Level.Verbose, string.Format("{0} : {1}-{2}-{3} {4}:{5}:{6}", "SetTime", lstTimeData[0], lstTimeData[1], lstTimeData[2], lstTimeData[3], lstTimeData[4], lstTimeData[5]), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, this.Name, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }
        }

        protected string GetEIOState(ushort ProcState)
        {
            string strProcState = string.Empty;

            if (ProcState > 100)
                return "U";

            for (int i = 0; i < _dicEioStateStnd.Count; i++)
            {
                if (_dicEioStateStnd[i].EQPSTATE == ProcState.ToString())
                {
                    strProcState = _dicEioStateStnd[i].EIOSTATE;
                }
            }

            if (strProcState == string.Empty)
            {
                EIFLog(Level.Verbose, string.Format("{0} : Unknown EioState", ProcState), strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
            }

            return strProcState;
        }

        string SetErrDataSet(CVariable InData)
        {
            string strErrDataSet = string.Empty;

            DataSet dsError = new DataSet();
            DataRow drError = null;

            for (int i = 0; i < InData.StructureInfo.Variables.Count; i++)
            {
                dsError.Tables.Add(InData.StructureInfo.Variables[i].Name);

                for (int j = 0; j < InData.StructureInfo.Variables[i].Variables.Count; j++)
                {
                    dsError.Tables[InData.StructureInfo.Variables[i].Name].Columns.Add(InData.StructureInfo.Variables[i].Variables[j].Name, typeof(string));
                }

                for (int j = 0; j < InData.Structure.Variables[i].StructureList.Count; j++)
                {
                    drError = dsError.Tables[InData.StructureInfo.Variables[i].Name].NewRow();

                    for (int k = 0; k < InData.Structure.Variables[i].StructureList[j].Variables.Count; k++)
                    {
                        if (InData.Structure.Variables[i].StructureList[j].Variables[k].Value == null)
                            drError[InData.Structure.Variables[i].StructureList[j].Variables[k].Name] = string.Empty;
                        else
                            drError[InData.Structure.Variables[i].StructureList[j].Variables[k].Name] = InData.Structure.Variables[i].StructureList[j].Variables[k].Value.ToString();


                    }

                    dsError.Tables[InData.Structure[i].Name].Rows.Add(drError);
                }
            }

            StringBuilder sbError = new StringBuilder();
            System.IO.StringWriter swError = new System.IO.StringWriter(sbError);


            dsError.WriteXml(swError);
            swError.Flush();

            strErrDataSet = sbError.ToString();

            return strErrDataSet;
        }

        protected void EIFLog(Level level, string strLog, string strLogCategory, bool bInsertDB, string strEqpID, string strShopID)
        {
            Logger.Log(level, strLog, strLogCategory, strLogCategory);

            if (bInsertDB)
            {
                DataPack dpLog = new DataPack();

                using (CDataManager dmLog = new CDataManager("LOG"))
                {
                    dpLog.AddProperty("EVENT_TIME", typeof(DateTime), DateTime.Now);
                    dpLog.AddProperty("EQP_ID", typeof(string), strEqpID);
                    dpLog.AddProperty("LOG_LEVEL", typeof(string), level.ToString());
                    dpLog.AddProperty("LOG_TEXT", typeof(string), strLog);

                    dmLog.Create(string.Format("TB_{0}_EIF_LOG", strShopID), dpLog);
                }
            }
        }

        protected void SOLLog(Level level, string strLog, string strLogCategory, string strEqpID, string strShopID, string txnID, bool fileLogUse = true)
        {
            if (fileLogUse) Logger.Log(level, strLog, strLogCategory + "_SOL", strEqpID, txnID);
            else CExecutor.MoMLogger.Log(level, strLog, strLogCategory, strEqpID, txnID);
        }

        protected void SOLExLog(string strLog, string strLogCategory, string strEqpID, string strShopID, string txnID, bool fileLogUse = true)
        {
            if (fileLogUse) Logger.Log(Level.Exception, $"[STEP_0] {strLog.ToString()}", strLogCategory + "_SOL", strEqpID, txnID);
            else CExecutor.MoMLogger.Log(Level.Exception, $"[STEP_0] {strLog.ToString()}", strLogCategory, strEqpID, txnID);
        }

        protected void EIFLog(Level level, string strLog, string strTestMode, string strLogCategory, bool bInsertDB, string strEqpID, string strShopID)
        {
            strLog = "[" + strTestMode + "] " + strLog;
            Logger.Log(level, strLog, strLogCategory, strLogCategory);

            if (bInsertDB)
            {
                DataPack dpLog = new DataPack();

                using (CDataManager dmLog = new CDataManager("LOG"))
                {
                    dpLog.AddProperty("EVENT_TIME", typeof(DateTime), DateTime.Now);
                    dpLog.AddProperty("EQP_ID", typeof(string), strEqpID);
                    dpLog.AddProperty("LOG_LEVEL", typeof(string), level.ToString());
                    dpLog.AddProperty("LOG_TEXT", typeof(string), strTestMode + ":" + strLog);

                    dmLog.Create(string.Format("TB_{0}_EIF_LOG", strShopID), dpLog);
                }
            }
        }

        protected static void ExceptionLog(Logger logger, Exception ex, string strFrom, bool bInsertDB, string strEqpID, string strShopID)
        {
            if (logger != null)
                logger.Log(Level.Debug, string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace), "Exception", strFrom);
            else
                SystemLogger.Log(Level.Debug, string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace), "Exception", strFrom);

            if (bInsertDB)
            {
                DataPack dpLog = new DataPack();

                using (CDataManager dmLog = new CDataManager("LOG"))
                {
                    dpLog.AddProperty("EVENT_TIME", typeof(DateTime), DateTime.Now);
                    dpLog.AddProperty("EQP_ID", typeof(string), strEqpID);
                    dpLog.AddProperty("LOG_LEVEL", typeof(string), "Exception");
                    dpLog.AddProperty("LOG_TEXT", typeof(string), string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace));

                    dmLog.Create(string.Format("TB_{0}_EIF_LOG", strShopID), dpLog);
                }
            }
        }

        //$ 2024.11.28 : Biz 호출 Method 통합
        protected int BizCall(string bizName, string strLogCat, CStructureVariable inVariable, CStructureVariable outVariable, out Exception bizEx, string txnID = "", bool bLogging = true)
        {
            int iRet = -1;
            DateTime preTime = DateTime.Now;

            if (!string.IsNullOrEmpty(txnID)) //$ 2025.05.14 : Solace Log는 txnID가 없는 경우 남기지 않음
                SOLLog(Level.Info, $"[STEP_3] {bizName} {inVariable.Variable.ToString()}", strLogCategory, strLogCat, SHOPID.FORM, txnID);

            this.BASE.EnableLoggingBizRule = bLogging;

            if (SIMULATION_MODE) //$ 2025.01.21
            {
                EIFLog(Level.Verbose, $"SIMUL MODE : {SIMULATION_MODE} - {bizName} return OK", strLogCategory, false, this.BASE.Variables["BASICINFO:V_EQP_ID_01"].AsString, SHOPID.FORM);
                bizEx = null;
                iRet = 0;
            }
            else
            {
                iRet = this.BASE.RequestQueueBR_Variable(this.ReqQueue, this.RepQueue, bizName, inVariable, outVariable, this.BizTimeout, out bizEx);
            }

            this.BASE.EnableLoggingBizRule = true;

            if (!string.IsNullOrEmpty(txnID)) //$ 2025.05.14 : Solace Log는 txnID가 없는 경우 남기지 않음
                SOLLog(Level.Info, $"[STEP_4] {bizName} [{(DateTime.Now - preTime).TotalMilliseconds:0.0}ms] - {iRet}", strLogCategory, strLogCat, SHOPID.FORM, txnID);


            if (iRet == 0 && !string.IsNullOrEmpty(txnID) && outVariable != null)
                SOLLog(Level.Info, $"[STEP_4] {bizName} : {outVariable.Variable.ToString()} - {iRet}", strLogCategory, strLogCat, SHOPID.FORM, txnID);

            return iRet;
        }

        public string GetHostIP(string connectionInfo)
        {
            string ip = "127.0.0.1";
            try
            {
                Regex ex = new Regex(@"HOST(.+?)\,", RegexOptions.IgnoreCase);
                string tmp = ex.Match(connectionInfo).Groups[1].Value;
                ip = tmp.Replace("=", "").Trim();
            }
            catch { }

            return ip;
        }
        #endregion


        #region Solace Log용 Method 
        public string GenerateTransactionKey()
        {
            var sb = new StringBuilder(26);

            // 1. 현재시간 활용 (17자리)
            sb.Append(DateTime.Now.ToString("yyyyMMddHHmmssfff"));

            // 2. 프로세스 ID와 스레드 ID를 활용 (5자리)
            int processId = Process.GetCurrentProcess().Id % 1000;  // 최대 3자리
            int threadId = Environment.CurrentManagedThreadId % 100; // 최대 2자리

            sb.AppendFormat($"{0:D3}{1:D2}", processId, threadId);

            // 3. 랜덤 숫자 (4자리)
            sb.Append(GenerateRandomDigits(4));

            return sb.ToString();
        }

        public string GenerateRandomDigits(int length)
        {
            char[] result = new char[length];
            byte[] randomBytes = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            for (int i = 0; i < length; i++)
            {
                result[i] = (char)('0' + (randomBytes[i] % 10));
            }

            return new string(result);
        }
        #endregion


        #region Mointoring
        private void EIFMonitoringData()
        {
            try
            {
                this.MONITOR__V_MONITOR_SOLACE = this.BASE.ConnectionState.ToString();
                if (this.BASE.Drivers.Count >= 1)
                {
                    if (this.BASE.Drivers[1].ConnectionState == enumConnectionState.Connected)
                        this.MONITOR__V_MONITOR_PLC_COMMNUICATION = PLCConnectionState.ONLINE.ToString();
                    else
                        this.MONITOR__V_MONITOR_PLC_COMMNUICATION = PLCConnectionState.OFFLINE.ToString();
                }

                this.MONITOR__V_MONITOR_EQPSTATUS = GetStringEqpStat(this.UNIT.EQP_STAT_CHG_RPT__I_W_EQP_STAT);
            }
            catch (Exception ex)
            {
                ExceptionLog(Logger, ex, Name, false, strEqpID, SHOPID.FORM);
            }
        }

        private string GetStringEqpStat(ushort eqpState)
        {
            string result = string.Empty;

            switch (eqpState)
            {
                case 0:
                    result = "Power Off";
                    break;
                case 1:
                    result = "Run";
                    break;
                case 2:
                    result = "Wait";
                    break;
                case 4:
                    result = "Trouble";
                    break;
                case 8:
                    result = "User Stop";
                    break;
            }

            return result;
        }
        #endregion
    }
}