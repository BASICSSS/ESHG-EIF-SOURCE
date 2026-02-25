using System;
using System.Collections.Generic;

using LGCNS.ezControl.Core;

using ESHG.EIF.FORM.COMMON;

namespace ESHG.EIF.FORM.JIGLDULD
{
    public partial class CJIGLDULD_BIZ
    {
        #region LD Station Tray Exist
        protected virtual void __G2_1_CARR_ID_RPT_01__I_B_TRAY_EXIST_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(sender, System.Reflection.MethodBase.GetCurrentMethod().Name + $" : [{(value ? "ON" : "OFF")}]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }
        #endregion

        #region LD Station Tray ID Check Request
        protected virtual void __G2_1_CARR_ID_RPT_01__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_1_CARR_ID_RPT_01__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_1_CARR_ID_RPT_01__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_1_CARR_ID_RPT_01__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(sender, eventName + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_LOT_ID = string.Empty;  //$ 2021.09.15 : 누락되어 추가
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_GROUP_LOT_ID = string.Empty;  //$ 2019.05.28 : 누락되어 추가
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_PRODUCT_ID = string.Empty;
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_SPECIAL_INFO = string.Empty;
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_SPECIAL_NO = string.Empty;
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_ROUTE_ID = string.Empty;
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_CHANNEL_CNT = 0;
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_CELL_SIZE = string.Empty;
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_CELL_CNT = 0;
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_LOT_TYPE = string.Empty;

                    _EIFServer.SetVarStatusLog(sender, $"LD Station Tray ID Confirm : [OFF]");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //EQP 상태 확인 
                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.EQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                if (!this.BASE.G2_1_CARR_ID_RPT_01__I_B_TRAY_EXIST)
                {
                    _EIFServer.SetVarStatusLog(sender, $"Invalid EQP Status!! LD Tray Exist : [{(this.BASE.G2_1_CARR_ID_RPT_01__I_B_TRAY_EXIST ? "EXIST" : "EMPTY")}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                string trayID = IsHR ? this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID);

                //BizRule 입력데이터 체크
                if (String.IsNullOrEmpty(trayID))
                {
                    HostAlarm(sender, EIFALMCD.JIGEMPTYTRAYID.ToString(), $"LD_TRAY_BCR_ST_Tray ID is Null or Empty", 1);

                    _EIFServer.SetVarStatusLog(sender, $"LD Station Host Trouble : [ON]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID))} : {trayID}");

                int iRst = 0;
                Exception bizEx = null;
                String strBizName = "BR_GET_JIG_FORM_LD_TRAY_INFO";

                CBR_GET_JIG_FORM_LD_TRAY_INFO_IN inData = CBR_GET_JIG_FORM_LD_TRAY_INFO_IN.GetNew(this);
                CBR_GET_JIG_FORM_LD_TRAY_INFO_OUT outData = CBR_GET_JIG_FORM_LD_TRAY_INFO_OUT.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPTID = this.EQPID;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].CSTID = trayID;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EMPTY_YN = this.BASE.G2_1_CARR_ID_RPT_01__I_B_BUFFER_EMPTY_YN ? "Y" : "N";

                string LDBufferCellId = "";
                List<string> arrCellIDList = IsHR ? this.BASE.G2_3_CARR_OUT_RPT_04__I_W_CELL_ID_LIST : this.BASE.GetSimLists(() => this.BASE.G2_3_CARR_OUT_RPT_04__I_W_CELL_ID_LIST); //$ 2024.03.07 : 배열에 대한 값을 Read/Write할때 따로 변수 처리를 해야 부하나 속도 이슈가 없다고 함
                                                                                                                                                                                      //$ 2024.12.30 : GMES 통합 테스트를 위해서 변수 변경

                if (!this.BASE.G2_1_CARR_ID_RPT_01__I_B_BUFFER_EMPTY_YN)
                {
                    for (int i = this.BASE.EQP_INFO__V_TRAYINCELLCNT - 1; i >= 0; i--) //Load Buffer에 Cell이 빈 상태가 아니라면 맨 마지막 Buffer의 CELLID을 찾아서 보고(Tray를 이어서 작업시) //$ 2021.12.15 : Virtual 변수에서 Cell 전체 수량 받아옴
                    {
                        if (!String.IsNullOrEmpty(arrCellIDList[i].Trim()) && !"DUMMY".Equals(arrCellIDList[i].Trim()))
                        {
                            LDBufferCellId = arrCellIDList[i].Trim();
                            break;
                        }
                    }
                }

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].BUFFER_SUBLOTID = LDBufferCellId;

                //입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} No Input Data!! : TRAY ID=[{trayID}], EMPTY YN=[{this.BASE.G2_1_CARR_ID_RPT_01__I_B_BUFFER_EMPTY_YN}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, nameof(O_B_REP), true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 1);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                //BizRule 정상 처리 후
                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_CHANNEL_CNT = IsRl ? Convert.ToUInt16(outData.OUTDATA[0].CHANNEL_CNT) : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_CHANNEL_CNT);
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_CELL_CNT = IsRl ? Convert.ToUInt16(outData.OUTDATA[0].SUBLOT_CNT) : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_CELL_CNT);
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_CELL_SIZE = IsRl ? outData.OUTDATA[0].SUBLOT_SIZE : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_CELL_SIZE);
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_SPECIAL_INFO = IsRl ? (string.IsNullOrEmpty(outData.OUTDATA[0].SPCL_FLAG.Trim()) ? "0" : outData.OUTDATA[0].SPCL_FLAG.Trim()) : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_SPECIAL_INFO);
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_SPECIAL_NO = IsRl ? (string.IsNullOrEmpty(outData.OUTDATA[0].FORM_SPCL_GR_ID.Trim()) ? "0" : outData.OUTDATA[0].FORM_SPCL_GR_ID.Trim()) : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_SPECIAL_NO);
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_ROUTE_ID = IsRl ? outData.OUTDATA[0].ROUTID : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_ROUTE_ID);
                    //this.BASE.G2_1_CARR_ID_RPT_01__O_W_GROUP_LOT_ID = IsRl ? outData.OUTDATA[0].LOTID.Substring(0, 8) : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_GROUP_LOT_ID);
                    //$ 2024.11.05 : 일 그룹 LOT ID OUTDATA 로 받아오기 때문에, Substring 진행할 필요가 없음
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_GROUP_LOT_ID = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_GROUP_LOT_ID);
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_LOT_ID = IsRl ? outData.OUTDATA[0].LOTID : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_LOT_ID); //2021.09.15 New
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_PRODUCT_ID = IsRl ? outData.OUTDATA[0].PRODID : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_PRODUCT_ID);
                    this.BASE.G2_1_CARR_ID_RPT_01__O_W_LOT_TYPE = IsRl ? outData.OUTDATA[0].LOTTYPE : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__O_W_LOT_TYPE);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);

                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Success : Tray ID=[{trayID}], EMPTY YN=[{this.BASE.G2_1_CARR_ID_RPT_01__I_B_BUFFER_EMPTY_YN}]");
                }
                else
                {
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Fail : Tray ID=[{trayID}], EMPTY YN=[{this.BASE.G2_1_CARR_ID_RPT_01__I_B_BUFFER_EMPTY_YN}]");
                }

                _EIFServer.SetVarStatusLog(sender, $"LD Station Tray ID Confirm : [ON]");

                _EIFServer.MhsReport_LoadedCarrier(SIMULATION_MODE, this.EQPID, trayID);
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_1_CARR_ID_RPT_01__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_1_CARR_ID_RPT_01__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion

        #region LD Job Start
        protected virtual void __G2_2_CARR_JOB_START_RPT_01__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_2_CARR_JOB_START_RPT_01__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_2_CARR_JOB_START_RPT_01__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_2_CARR_JOB_START_RPT_01__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(sender, eventName + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(sender, "LD Job Start Confirm  : [OFF]");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //EQP 상태 확인
                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.EQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                if (!this.BASE.G2_1_CARR_ID_RPT_01__I_B_TRAY_EXIST)
                {
                    _EIFServer.SetVarStatusLog(sender, $"Invalid EQP Status!! LD Tray Exist : [{(this.BASE.G2_1_CARR_ID_RPT_01__I_B_TRAY_EXIST ? "EXIST" : "EMPTY")}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                string trayID = IsHR ? this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID);

                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID))} : {trayID}");

                int iRst = 0;
                Exception bizEx = null;
                String strBizName = "BR_SET_JIG_FORM_LD_TRAY_START";

                CBR_SET_JIG_FORM_LD_TRAY_START_IN inData = CBR_SET_JIG_FORM_LD_TRAY_START_IN.GetNew(this);
                CBR_SET_JIG_FORM_LD_TRAY_START_OUT outData = CBR_SET_JIG_FORM_LD_TRAY_START_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayID;

                //입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} No Input Data!! : TRAY ID=[{trayID}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 1);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                //BizRule 정상 처리 후
                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Success : Tray ID=[{trayID}]");
                }
                else
                {
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Fail : Tray ID=[{trayID}]");
                }

                _EIFServer.SetVarStatusLog(sender, "LD Job Start Confirm  : [ON]");

            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_2_CARR_JOB_START_RPT_01__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_2_CARR_JOB_START_RPT_01__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion

        #region LD Transfer Cell ID Check Request
        protected virtual void __G4_6_CELL_INFO_REQ_01__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G4_6_CELL_INFO_REQ_01__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G4_6_CELL_INFO_REQ_01__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G4_6_CELL_INFO_REQ_01__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(sender, eventName + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    this.BASE.G4_6_CELL_INFO_REQ_01__O_W_LOT_ID_1ST = "";
                    this.BASE.G4_6_CELL_INFO_REQ_01__O_W_GROUP_LOT_ID_1ST = "";
                    this.BASE.G4_6_CELL_INFO_REQ_01__O_W_ROUTE_ID_1ST = "";
                    this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_INFO_1ST = "";
                    this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_NO_1ST = "";

                    this.BASE.G4_6_CELL_INFO_REQ_01__O_W_LOT_ID_2ND = "";
                    this.BASE.G4_6_CELL_INFO_REQ_01__O_W_GROUP_LOT_ID_2ND = "";
                    this.BASE.G4_6_CELL_INFO_REQ_01__O_W_ROUTE_ID_2ND = "";
                    this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_INFO_2ND = "";
                    this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_NO_2ND = "";

                    _EIFServer.SetVarStatusLog(sender, "LD Transfer Cell ID Confirm : [OFF]");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //EQP 상태 확인
                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.EQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                _EIFServer.SetVarStatusLog(sender, $"LD Transfer Cell ID 1 : [{this.BASE.G4_6_CELL_INFO_REQ_01__I_W_CELL_ID_1ST}], LD Transfer Cell ID 2 : [{this.BASE.G4_6_CELL_INFO_REQ_01__I_W_CELL_ID_2ND}]");

                ushort cellHostTroublePosition = 0;
                int cellNotExistPosition = -1; //$ 2018.12.11 : 2CELL P&P에서 Cell이 없는 위치를 저장할 변수(-1:2Cell, 0:First Cell Nothing, 1:Sencond Cell Nothing)                

                //$ 2019.06.10 : Log 남기기 위한 값이므로 이전 Logic은 삭제
                if (string.IsNullOrEmpty(this.BASE.G4_6_CELL_INFO_REQ_01__I_W_CELL_ID_1ST.Trim()))
                    cellHostTroublePosition += (ushort)Math.Pow(2, 15);

                if (string.IsNullOrEmpty(this.BASE.G4_6_CELL_INFO_REQ_01__I_W_CELL_ID_2ND.Trim()))
                    cellHostTroublePosition += (ushort)Math.Pow(2, 14);

                string LDBufferEmptyYNForTest = this.BASE.G4_6_CELL_INFO_REQ_01__I_B_BUFFER_EMPTY_YN ? "Y" : "N";
                _EIFServer.SetVarStatusLog(sender, $"LD Transfer Cell ID PreCheck=[{Convert.ToString(cellHostTroublePosition, 2)}], LD Buffer Empty=[{LDBufferEmptyYNForTest}]");
                cellHostTroublePosition = 0; //초기화

                string LDBufferEmptyYN = this.BASE.G4_6_CELL_INFO_REQ_01__I_B_BUFFER_EMPTY_YN ? "Y" : "N";
                string LDBufferCellId = "";
                List<string> arrCellIDList = this.BASE.G2_3_CARR_OUT_RPT_04__I_W_CELL_ID_LIST; //$ 2024.03.07 : 배열에 대한 값을 Read/Write할때 따로 변수 처리를 해야 부하나 속도 이슈가 없다고 함

                if (!this.BASE.G4_6_CELL_INFO_REQ_01__I_B_BUFFER_EMPTY_YN)
                {
                    for (int i = this.BASE.EQP_INFO__V_TRAYINCELLCNT - 1; i >= 0; i--)    //$ 2021.12.15 : Virtual 변수에서 Cell 전체 수량 받아옴
                    {
                        if (!String.IsNullOrEmpty(arrCellIDList[i].Trim()) && !"DUMMY".Equals(arrCellIDList[i].Trim()))
                        {
                            LDBufferCellId = arrCellIDList[i].Trim();
                            break;
                        }
                    }
                }

                string trayID = IsHR ? this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID);
                string cellID_1ST = IsHR ? this.BASE.G4_6_CELL_INFO_REQ_01__I_W_CELL_ID_1ST.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__I_W_CELL_ID_1ST);
                string cellID_2ND = IsHR ? this.BASE.G4_6_CELL_INFO_REQ_01__I_W_CELL_ID_2ND.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__I_W_CELL_ID_2ND);

                //TODO $ 2025.05.14 : 이거 Cell 있는 부분만 CellID를 추출해야 한다면 조건 추가 필요
                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G4_6_CELL_INFO_REQ_01__I_W_CELL_ID_1ST))} : {cellID_1ST}, {GetDesc(nameof(this.BASE.G4_6_CELL_INFO_REQ_01__I_W_CELL_ID_2ND))} : {cellID_2ND}");

                int iRst = 0;
                String strBizName = "BR_GET_JIG_FORM_LD_CELL_CHECK";
                Exception bizEx = null;

                CBR_GET_JIG_FORM_LD_CELL_CHECK_IN inData = CBR_GET_JIG_FORM_LD_CELL_CHECK_IN.GetNew(this);
                CBR_GET_JIG_FORM_LD_CELL_CHECK_OUT outData = CBR_GET_JIG_FORM_LD_CELL_CHECK_OUT.GetNew(this);
                inData.INDATA_LENGTH = 0;

                int _pos = 0;
                for (ushort i = 0; i < this.BASE.EQP_INFO__V_PNPACTCELLCNT; i++)     //$ 2021.12.15 : P&P가 한번에 Pick & Place 할 수 있는 수량을 Virtual 변수로 얻어옴
                {
                    string cellID = string.Empty;

                    if (i == 0)
                        cellID = cellID_1ST;
                    else if (i == 1)
                        cellID = cellID_2ND;

                    //CELLID가 NULL일 경우 INDATA에 넣지 않으며, 해당 위치를 저장한다.
                    if (string.IsNullOrWhiteSpace(cellID))
                    {
                        cellNotExistPosition = _pos;
                        continue;
                    }

                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].SUBLOTID = cellID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].EMPTY_YN = LDBufferEmptyYN;
                    inData.INDATA[inData.INDATA_LENGTH - 1].BUFFER_SUBLOTID = LDBufferCellId;
                    inData.INDATA[inData.INDATA_LENGTH - 1].SUBSLOTNO = (ushort)(i + 1);    //$ 2021.12.08 : GMES Cell 위치 구분을 위한 SlotNo 추가

                    _pos++;
                }

                //입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName}  No Input Data!! : TRAY_ID=[{trayID}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 2);

                    _EIFServer.SetVarStatusLog(sender, $"LD Transfer Cell Host Trouble Code=[{this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_ID}], {this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_MSG[0]}, TRAY ID=[{trayID}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                //BizRule 정상 처리 후
                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Success : Tray ID=[{trayID}]");

                    //Cell Trouble Info (0: Default, 1: Trouble)
                    string cellCheck = outData.OUTDATA[0].SUBLOTID_CHK;
                    _EIFServer.SetVarStatusLog(sender, $"Cell ID Check=[{cellCheck}]");

                    //Cell Exist = "Y" 에 대해서만 Trouble Position Write
                    //Loader Cell 보고 시 Cell 이 없을 경우 (BCR NOREAD 인 경우 NG 배출) 
                    //Host 보고 없이 다시 Loader Tray 에서 빈 Cell 을 채워서 읽은 후 Host 로 보고 한다.
                    for (int i = 0; i < cellCheck.Length; i++)
                    {
                        if (cellCheck.Substring(i, 1).ToString() == "1")
                            cellHostTroublePosition += (ushort)Math.Pow(2, 15 - i);
                    }

                    //Cell Request 에 정상 Confirm 일 경우 Trouble Position Reset 
                    //Cell Trouble Position 이 있을 경우 Cell Host Trouble Position에 쓰고 정상 Confirm -> 설비는 NG Cell 로 배출함. 
                    string log = (cellHostTroublePosition == 0) ? "LD Transfer Cell ID check -> All Cell OK" : "LD Transfer Cell ID check -> Host NG Cell Selecting";
                    _EIFServer.SetVarStatusLog(sender, log);

                    //$ 2021.12.08 : GMES에서 Biz에 대한 데이터를 Cell Slot별로 주기 때문에 불필요한 Logic은 모두 삭제
                    //$ 2021.12.15 : P&P가 한번에 Pick & Place 할 수 있는 수량을 Virtual 변수로 얻어옴
                    for (int i = 0; i < this.BASE.EQP_INFO__V_PNPACTCELLCNT; i++)
                    {
                        if (cellNotExistPosition == i) continue; // Cell이 없는 위치면 Skip함(-1:2Cell, 0:First Cell Nothing, 1:Sencond Cell Nothing)

                        if (i == 0)
                        {
                            this.BASE.G4_6_CELL_INFO_REQ_01__O_W_ROUTE_ID_1ST = IsRl ? outData.OUTDATA[0].ROUTID_1ST : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__O_W_ROUTE_ID_1ST);
                            this.BASE.G4_6_CELL_INFO_REQ_01__O_W_GROUP_LOT_ID_1ST = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID_1ST : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__O_W_GROUP_LOT_ID_1ST);
                            this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_INFO_1ST = IsRl ? outData.OUTDATA[0].SPCL_FLAG_1ST : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_INFO_1ST);
                            this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_NO_1ST = IsRl ? outData.OUTDATA[0].SPECIAL_NO_1ST : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_NO_1ST);
                            this.BASE.G4_6_CELL_INFO_REQ_01__O_W_LOT_ID_1ST = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID_1ST : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__O_W_LOT_ID_1ST);

                            _EIFServer.SetVarStatusLog(sender, $"LD Transfer Cell[{i}] Route LOT ID = [{outData.OUTDATA[0].ROUTID_1ST}, {outData.OUTDATA[0].DAY_GR_LOTID_1ST}]");
                        }
                        else
                        {
                            this.BASE.G4_6_CELL_INFO_REQ_01__O_W_ROUTE_ID_2ND = IsRl ? outData.OUTDATA[0].ROUTID_2ND : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__O_W_ROUTE_ID_2ND);
                            this.BASE.G4_6_CELL_INFO_REQ_01__O_W_GROUP_LOT_ID_2ND = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID_2ND : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__O_W_GROUP_LOT_ID_2ND);
                            this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_INFO_2ND = IsRl ? outData.OUTDATA[0].SPCL_FLAG_2ND : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_INFO_2ND);
                            this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_NO_2ND = IsRl ? outData.OUTDATA[0].SPECIAL_NO_2ND : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__O_W_SPECIAL_NO_2ND);
                            this.BASE.G4_6_CELL_INFO_REQ_01__O_W_LOT_ID_2ND = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID_2ND : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_01__O_W_LOT_ID_2ND);

                            _EIFServer.SetVarStatusLog(sender, $"LD Transfer Cell[{i}] Route LOT ID = [{outData.OUTDATA[0].ROUTID_2ND}, {outData.OUTDATA[0].DAY_GR_LOTID_2ND}]");
                        }
                    }

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);
                }
                else
                {
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Fail : Tray ID=[{trayID}]");
                }

                _EIFServer.SetVarStatusLog(sender, "LD Transfer Cell ID Confirm : [ON]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G4_6_CELL_INFO_REQ_01__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G4_6_CELL_INFO_REQ_01__O_B_TRIGGER_REPORT_CONF = true;

            }
        }
        #endregion

        #region LD Buffer Transfer Complete
        protected virtual void __G2_3_CARR_OUT_RPT_04__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_3_CARR_OUT_RPT_04__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_3_CARR_OUT_RPT_04__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_3_CARR_OUT_RPT_04__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(sender, eventName + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    this.BASE.G2_3_CARR_OUT_RPT_04__O_W_JIG_ID = string.Empty;

                    _EIFServer.SetVarStatusLog(sender, "LD Buffer Transfer Complete Confirm  : [OFF]");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //EQP 상태 확인             
                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.EQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                //LD Buffer CELL Count 체크
                _EIFServer.SetVarStatusLog(sender, $"LD Buffer Cell Count=[{this.BASE.G2_3_CARR_OUT_RPT_04__I_W_CELL_CNT}]");
                if (this.BASE.G2_3_CARR_OUT_RPT_04__I_W_CELL_CNT != this.BASE.EQP_INFO__V_TRAYINCELLCNT)     //$ 2021.12.15 : Virtual 변수에서 Cell 전체 수량 받아옴
                {
                    HostAlarm(sender, EIFALMCD.JIGCELLCNT.ToString(), $"LD_BUFFER_Invalid Cell Count : {this.BASE.G2_3_CARR_OUT_RPT_04__I_W_CELL_CNT}", 3);

                    _EIFServer.SetVarStatusLog(sender, $"LD Buffer Station Host Trouble : [ON]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                string trayID = IsHR ? this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID);
                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_1_CARR_ID_RPT_01__I_W_TRAY_ID))} : {trayID}");

                List<string> arrCellIDList = this.BASE.G2_3_CARR_OUT_RPT_04__I_W_CELL_ID_LIST; //$ 2024.03.07 : 배열에 대한 값을 Read/Write할때 따로 변수 처리를 해야 부하나 속도 이슈가 없다고 함

                //CELL ID 공백 또는 NOREAD 일 경우 체크
                for (int i = 0; i < this.BASE.EQP_INFO__V_TRAYINCELLCNT; i++)    //$ 2021.12.15 : Virtual 변수에서 Cell 전체 수량 받아옴
                {
                    if (String.IsNullOrEmpty(arrCellIDList[i].Trim()) || "NOREAD".Equals(arrCellIDList[i].Trim()))
                        _EIFServer.SetVarStatusLog(sender, $"LD Buffer Cell[{i}]'s ID=[{arrCellIDList[i]}] is invalid!!");
                }

                int iRst = 0;
                Exception bizEx = null;
                string strBizName = "BR_SET_JIG_FORM_LD_BUFFER_COMPLETE";

                CBR_SET_JIG_FORM_LD_BUFFER_COMPLETE_IN inData = CBR_SET_JIG_FORM_LD_BUFFER_COMPLETE_IN.GetNew(this);
                CBR_SET_JIG_FORM_LD_BUFFER_COMPLETE_OUT outData = CBR_SET_JIG_FORM_LD_BUFFER_COMPLETE_OUT.GetNew(this);
                inData.INDATA_LENGTH = 0;

                for (int i = 0; i < this.BASE.EQP_INFO__V_TRAYINCELLCNT; i++)    //$ 2021.12.15 : Virtual 변수에서 Cell 전체 수량 받아옴
                {
                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].SUBLOTID = IsHR ? arrCellIDList[i].Trim() : _EIFServer.GetSimArrValue(() => this.BASE.G2_3_CARR_OUT_RPT_04__I_W_CELL_ID_LIST, i); //$ 2024.03.07 : 기존 Factova 변수에 배열값을 직접 Read하던 부분을 지역변수로 변경했음, SimulData는 PLC와 무관하므로 기존 방식 유지
                    inData.INDATA[inData.INDATA_LENGTH - 1].CSTID_AF = string.Empty;
                }

                //입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} No Input Data!! : TRAY ID=[{trayID}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 3);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                //BizRule 정상 처리 후
                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    this.BASE.G2_3_CARR_OUT_RPT_04__O_W_JIG_ID = IsRl ? outData.OUTDATA[0].CSTID : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_04__O_W_JIG_ID);

                    Wait(1000);

                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Success : Jig ID=[{this.BASE.G2_3_CARR_OUT_RPT_04__O_W_JIG_ID}], Tray ID=[{trayID}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);
                }
                else
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Fail : Jig ID=[{this.BASE.G2_3_CARR_OUT_RPT_04__O_W_JIG_ID}], Tray ID=[{trayID}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                }

                _EIFServer.SetVarStatusLog(sender, "LD Buffer Transfer Complete Confirm : [ON]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_3_CARR_OUT_RPT_04__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_3_CARR_OUT_RPT_04__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion

        #region LD Buffer Empty
        protected virtual void __G4_6_CELL_INFO_REQ_01__I_B_BUFFER_EMPTY_YN_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(sender, System.Reflection.MethodBase.GetCurrentMethod().Name + $" : [{(value ? "ON" : "OFF")}]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }
        #endregion

        #region LD Job Complete
        protected virtual void __G2_3_CARR_OUT_RPT_01__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_3_CARR_OUT_RPT_01__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_3_CARR_OUT_RPT_01__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_3_CARR_OUT_RPT_01__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(sender, eventName + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(sender, "LD Job Complete Confirm : [OFF]");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //EQP 상태 확인   
                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.EQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                string trayID = IsHR ? this.BASE.G2_3_CARR_OUT_RPT_01__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_01__I_W_TRAY_ID);

                //BizRule 입력데이터 체크
                if (string.IsNullOrEmpty(trayID))
                {
                    HostAlarm(sender, EIFALMCD.JIGEMPTYTRAYID.ToString(), $"LD_TRAY_BCR_ST_Tray ID is Null or Empty", 1);
                    _EIFServer.SetVarStatusLog(sender, $"LD Station Host Trouble : [ON]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_3_CARR_OUT_RPT_01__I_W_TRAY_ID))} : {trayID}");

                string log = null;
                int iRst = 0;
                Exception bizEx = null;
                string strBizName = "BR_SET_JIG_FORM_LD_TRAY_END_JOB";

                CBR_SET_JIG_FORM_LD_TRAY_END_JOB_IN inData = CBR_SET_JIG_FORM_LD_TRAY_END_JOB_IN.GetNew(this);
                CBR_SET_JIG_FORM_LD_TRAY_END_JOB_OUT outData = CBR_SET_JIG_FORM_LD_TRAY_END_JOB_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayID;

                //입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    log = $"{strBizName} No Input Data!! : TRAY ID=[{trayID}]";
                    _EIFServer.SetVarStatusLog(sender, log);

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 1);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                //BizRule 정상 처리 후
                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Success : Tray ID=[{trayID}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);
                }
                else
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Fail : Tray ID=[{trayID}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                }

                _EIFServer.SetVarStatusLog(sender, "LD Job Complete Confirm : [ON]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_3_CARR_OUT_RPT_01__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_3_CARR_OUT_RPT_01__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion
    }
}