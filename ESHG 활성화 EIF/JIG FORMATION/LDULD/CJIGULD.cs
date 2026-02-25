using System;
using System.Collections.Generic;

using LGCNS.ezControl.Core;
using ESHG.EIF.FORM.COMMON;

namespace ESHG.EIF.FORM.JIGLDULD
{
    public partial class CJIGLDULD_BIZ
    {
        #region ULD Station Tray Exist
        protected virtual void __G2_2_CARR_JOB_START_RPT_03__I_B_TRAY_EXIST_OnBooleanChanged(CVariable sender, bool value)
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

        #region ULD Station Tray ID Check Request
        protected virtual void __G2_2_CARR_JOB_START_RPT_03__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_2_CARR_JOB_START_RPT_03__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_2_CARR_JOB_START_RPT_03__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_2_CARR_JOB_START_RPT_03__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(sender, eventName + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(sender, $"ULD Station Tray ID Confirm : [OFF]");
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

                if (!this.BASE.G2_2_CARR_JOB_START_RPT_03__I_B_TRAY_EXIST)
                {
                    _EIFServer.SetVarStatusLog(sender, $"Invalid EQP Status!! ULD Tray Exist : [{(this.BASE.G2_2_CARR_JOB_START_RPT_03__I_B_TRAY_EXIST ? "EXIST" : "EMPTY")}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                string trayID = IsHR ? this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID);

                //BizRule 입력데이터 체크
                if (String.IsNullOrEmpty(trayID))
                {
                    HostAlarm(sender, EIFALMCD.JIGEMPTYTRAYID.ToString(), $"ULD_TRAY_BCR_ST_Tray ID is Null or Empty", 5);

                    _EIFServer.SetVarStatusLog(sender, $"ULD Station Host Trouble : [ON]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID))} : {trayID}");

                int iRst = 0;
                string strBizName = "BR_GET_JIG_FORM_TRAY_EMPTY_CHECK";
                Exception bizEx = null;

                CBR_GET_JIG_FORM_TRAY_EMPTY_CHECK_IN inData = CBR_GET_JIG_FORM_TRAY_EMPTY_CHECK_IN.GetNew(this);
                CBR_GET_JIG_FORM_TRAY_EMPTY_CHECK_OUT outData = CBR_GET_JIG_FORM_TRAY_EMPTY_CHECK_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayID;

                //입력 데이터가 없으면
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} No Input Data!! : TRAY ID=[{this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID}]");

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

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 5);

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

                _EIFServer.SetVarStatusLog(sender, "ULD Station Tray ID Confirm : [ON]");

                //$ 2022.12.21 : 설비로 투입되는 Tray에 대해 TrayID를 MHS로 보고하여 반송 종료 처리 함(공 Tray Port로 들어오든, 실 -> 공으로 바뀌거든 구분이 안되므로 다 보고 - 최종엽 책임님)
                _EIFServer.MhsReport_LoadedCarrier(SIMULATION_MODE, this.EQPID, trayID);
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_2_CARR_JOB_START_RPT_03__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_2_CARR_JOB_START_RPT_03__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion

        #region ULD Buffer Transfer Complete
        protected virtual void __G2_2_CARR_JOB_START_RPT_02__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_2_CARR_JOB_START_RPT_02__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_2_CARR_JOB_START_RPT_02__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(sender, eventName + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_GROUP_LOT_ID = string.Empty;
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_ROUTE_ID = string.Empty;
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_SPECIAL_INFO = string.Empty;
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_PRODUCT_ID = string.Empty;
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_CELL_EXIST_LIST = string.Empty;
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_CELL_GRADE_LIST = string.Empty;
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_LOT_TYPE = string.Empty;

                    // 2025.06.02 사양서에 없는 내용으로 임시 삭제
                    //this.BASE.ULD_TRAY__O_W_ULD_BUFFER_FORCE_OUT_FLAG = string.Empty;

                    _EIFServer.SetVarStatusLog(sender, $"ULD Buffer Transfer Complete Confirm : [OFF]");
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

                string jigID = IsHR ? this.BASE.G2_2_CARR_JOB_START_RPT_02__I_W_JIG_ID : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_02__I_W_JIG_ID);
                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_2_CARR_JOB_START_RPT_02__I_W_JIG_ID))} : {jigID}");

                int iRst = 0;
                string strBizName = "BR_SET_JIG_FORM_ULD_BUFFER_COMPLETE";
                Exception bizEx = null;

                CBR_SET_JIG_FORM_ULD_BUFFER_COMPLETE_IN inData = CBR_SET_JIG_FORM_ULD_BUFFER_COMPLETE_IN.GetNew(this);
                CBR_SET_JIG_FORM_ULD_BUFFER_COMPLETE_OUT outData = CBR_SET_JIG_FORM_ULD_BUFFER_COMPLETE_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = jigID;
                inData.INDATA[inData.INDATA_LENGTH - 1].DAY_GR_LOTID = string.Empty;

                //입력 데이터가 없으면
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} No Input Data!! : ULD Buffer Jig ID=[{jigID}]");

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

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 5);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                //BizRule 정상 처리 후
                if (iRst.Equals(0))
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Success :ULD Buffer Jig ID=[{jigID}]");

                    //$ 2019.05.24 : LOTID로 오는 정보를 각 Address별로 나눠준다.
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_ROUTE_ID = IsRl ? outData.OUTDATA[0].ROUTID : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_ROUTE_ID);
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_GROUP_LOT_ID = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID.Substring(0, 8) : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_GROUP_LOT_ID);     //대LOT 8
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_SPECIAL_INFO = IsRl ? outData.OUTDATA[0].SPCL_FLAG : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_SPECIAL_INFO);
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_SPECIAL_NO = IsRl ? outData.OUTDATA[0].FORM_SPCL_GR_ID : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_SPECIAL_NO);

                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_PRODUCT_ID = IsRl ? outData.OUTDATA[0].PRODID : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_PRODUCT_ID);
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_CELL_EXIST_LIST = IsRl ? outData.OUTDATA[0].SORTING_LIST : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_CELL_EXIST_LIST);
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_CELL_GRADE_LIST = IsRl ? outData.OUTDATA[0].SUBLOTJUDGE_LIST : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_CELL_GRADE_LIST);

                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_LOT_ID = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_LOT_ID);
                    this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_LOT_TYPE = IsRl ? outData.OUTDATA[0].LOTTYPE : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_LOT_TYPE);

                    // 2025.06.02 사양서에 없는 내용으로 임시 삭제
                    //this.BASE.ULD_TRAY__O_W_FORCE_OUT_FLAG = IsRl ? outData.OUTDATA[0].FORCE_OUT_FLAG : _EIFServer.GetSimValue(() => this.BASE.ULD_TRAY__O_W_FORCE_OUT_FLAG);

                    _EIFServer.SetVarStatusLog(sender, $"ULD Buffer Route_Lot_ID=[{this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_ROUTE_ID}], Cells Grade Choice=[{this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_CELL_EXIST_LIST}], Cells Grade=[{this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_CELL_GRADE_LIST}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);
                }
                else
                {
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);

                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Fail : ULD Buffer Jig ID=[{jigID}]");
                }

                _EIFServer.SetVarStatusLog(sender, $"ULD Buffer Transfer Complete Confirm : [ON]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_2_CARR_JOB_START_RPT_02__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_2_CARR_JOB_START_RPT_02__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion

        #region ULD Transfer Cell ID Check Request
        protected virtual void __G4_6_CELL_INFO_REQ_03__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G4_6_CELL_INFO_REQ_03__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G4_6_CELL_INFO_REQ_03__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G4_6_CELL_INFO_REQ_03__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(sender, eventName + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    //LHS 2023.10.13 : 생산팀 요청으로 인한 ULD CELL TRANSFER 특관 SPECIAL_INFO, NO 추가
                    this.BASE.G4_6_CELL_INFO_REQ_03__O_W_LOT_ID_1ST = "";
                    this.BASE.G4_6_CELL_INFO_REQ_03__O_W_GROUP_LOT_ID_1ST = "";
                    this.BASE.G4_6_CELL_INFO_REQ_03__O_W_ROUTE_ID_1ST = "";
                    //this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_1ST = ""; 
                    //this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_NO_1ST = "";

                    this.BASE.G4_6_CELL_INFO_REQ_03__O_W_LOT_ID_2ND = "";
                    this.BASE.G4_6_CELL_INFO_REQ_03__O_W_GROUP_LOT_ID_2ND = "";
                    this.BASE.G4_6_CELL_INFO_REQ_03__O_W_ROUTE_ID_2ND = "";
                    //this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_2ND = "";
                    //this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_NO_2ND = "";

                    _EIFServer.SetVarStatusLog(sender, "ULD Transfer Cell ID Confirm : [OFF]");
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

                string cellID_1ST = IsHR ? this.BASE.G4_6_CELL_INFO_REQ_03__I_W_CELL_ID_1ST.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_03__I_W_CELL_ID_1ST);
                string cellID_2ND = IsHR ? this.BASE.G4_6_CELL_INFO_REQ_03__I_W_CELL_ID_2ND.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_03__I_W_CELL_ID_2ND);

                //TODO $ 2025.05.14 : 이거 Cell 있는 Gripper만 CellID를 추출해야 한다면 조건 추가 필요
                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G4_6_CELL_INFO_REQ_03__I_W_CELL_ID_1ST))} : {cellID_1ST}, {GetDesc(nameof(this.BASE.G4_6_CELL_INFO_REQ_03__I_W_CELL_ID_2ND))} : {cellID_2ND}");

                _EIFServer.SetVarStatusLog(sender, $"ULD Transfer Cell ID 1 : [{cellID_1ST}], ULD Transfer Cell ID 2 : [{cellID_2ND}]");

                ushort cellHostTroublePosition = 0;
                for (int i = 0; i < this.BASE.EQP_INFO__V_PNPACTCELLCNT; i++)     //$ 2021.12.15 : P&P가 한번에 Pick & Place 할 수 있는 수량을 Virtual 변수로 얻어옴
                {
                    if (i == 0 && String.IsNullOrEmpty(cellID_1ST))
                        cellHostTroublePosition += (ushort)Math.Pow(2, 15 - i);
                    else if (i == 1 && String.IsNullOrEmpty(cellID_2ND))
                        cellHostTroublePosition += (ushort)Math.Pow(2, 15 - i);
                }

                _EIFServer.SetVarStatusLog(sender, $"ULD Transfer Cell ID PreCheck=[{Convert.ToString(cellHostTroublePosition, 2)}]");
                cellHostTroublePosition = 0; //초기화

                int iRst = 0;
                string strBizName = "BR_GET_JIG_FORM_CELL_JUDGE_INFO";
                Exception bizEx = null;

                CBR_GET_JIG_FORM_CELL_JUDGE_INFO_IN inData = CBR_GET_JIG_FORM_CELL_JUDGE_INFO_IN.GetNew(this);
                CBR_GET_JIG_FORM_CELL_JUDGE_INFO_OUT outData = CBR_GET_JIG_FORM_CELL_JUDGE_INFO_OUT.GetNew(this);
                inData.INDATA_LENGTH = 0;

                int _pos = 0;
                int cellNotExistPosition = -1; //$ 2018.12.11 : 2CELL P&P에서 Cell이 없는 위치를 저장할 변수(-1:2Cell, 0:First Cell Nothing, 1:Sencond Cell Nothing)
                for (ushort i = 0; i < this.BASE.EQP_INFO__V_PNPACTCELLCNT; i++)  //$ 2021.12.15 : P&P가 한번에 Pick & Place 할 수 있는 수량을 Virtual 변수로 얻어옴
                {
                    string CellID = string.Empty;

                    if (i == 0)
                        CellID = cellID_1ST;
                    else if (i == 1)
                        CellID = cellID_2ND;

                    //CELLID가 NULL일 경우 INDATA에 넣지 않으며, 해당 위치를 저장한다.
                    if (string.IsNullOrWhiteSpace(CellID))
                    {
                        cellNotExistPosition = _pos;
                        continue;
                    }

                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].SUBLOTID = CellID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].SUBSLOTNO = (ushort)(i + 1); //$ 2021.12.08 : GMES Cell 위치 구분을 위한 SlotNo 추가

                    _pos++;
                }

                //입력 데이터가 없으면
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName}  No Input Data!! : ULD TRAY_ID=[{this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, inData, outData, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 6);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);

                    _EIFServer.SetVarStatusLog(sender, $"ULD Transfer Cell Host Trouble Code=[{this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_ID}], {this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_MSG[0]}, TRAY ID=[{this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID}]");
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                //BizRule 정상 처리 후
                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Success : Tray ID=[{this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID}]");

                    //Cell Trouble Info (0: Default, 1: Trouble)
                    string cellCheck = outData.OUTDATA[0].SUBLOTID_CHK;
                    string sortingInfo = outData.OUTDATA[0].SORTING_INFO;
                    string cellGrade = outData.OUTDATA[0].SUBLOTJUDGE;
                    string routeIDs = $"{outData.OUTDATA[0].ROUTID_1ST},{outData.OUTDATA[0].ROUTID_2ND}";
                    string grpLotIDs = $"{outData.OUTDATA[0].DAY_GR_LOTID_1ST},{outData.OUTDATA[0].DAY_GR_LOTID_2ND}";

                    _EIFServer.SetVarStatusLog(sender, $"ULD Cell ID Check=[{cellCheck}], SORTING INFO=[{sortingInfo}], CELL GRADE=[{cellGrade}], RouteIDs=[{routeIDs}] GroupLOTIDs=[{grpLotIDs}]");

                    for (int i = 0; i < cellCheck.Length; i++)
                    {
                        if (cellCheck.Substring(i, 1).ToString() == "1")
                            cellHostTroublePosition += (ushort)Math.Pow(2, 15 - i);
                    }

                    string log = (cellHostTroublePosition == 0) ? $"ULD Transfer Cell ID check -> All Cell OK" : $"ULD Transfer Cell ID check -> Host NG Cell Selecting";
                    _EIFServer.SetVarStatusLog(sender, log);

                    //$ 2021.12.08 : GMES에서 Biz에 대한 데이터를 Cell Slot별로 주기 때문에 불필요한 Logic은 모두 삭제
                    //$ 2021.12.15 : P&P가 한번에 Pick & Place 할 수 있는 수량을 Virtual 변수로 얻어옴
                    for (int i = 0; i < this.BASE.EQP_INFO__V_PNPACTCELLCNT; i++)
                    {
                        if (cellNotExistPosition == i) continue; // Cell이 없는 위치면 Skip함(-1:2Cell, 0:First Cell Nothing, 1:Sencond Cell Nothing)

                        if (i == 0)
                        {
                            this.BASE.G4_6_CELL_INFO_REQ_03__O_W_ROUTE_ID_1ST = IsRl ? outData.OUTDATA[0].ROUTID_1ST : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_03__O_W_ROUTE_ID_1ST);
                            this.BASE.G4_6_CELL_INFO_REQ_03__O_W_GROUP_LOT_ID_1ST = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID_1ST : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_03__O_W_GROUP_LOT_ID_1ST);
                            //this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_1ST = IsRl ? outData.OUTDATA[0].SPECIALINFO_1ST : _EIFServer.GetSimValue(() => this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_1ST); //LHS 2023.10.13 : 생산팀 요청으로 인한 ULD CELL TRANSFER 특관추가
                            //this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_NO_1ST = IsRl ? outData.OUTDATA[0].FORM_SPCL_GR_ID_1ST : _EIFServer.GetSimValue(() => this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_NO_1ST); //LHS 2023.10.13 : 생산팀 요청으로 인한 ULD CELL TRANSFER 특관추가
                            this.BASE.G4_6_CELL_INFO_REQ_03__O_W_LOT_ID_1ST = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID_1ST : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_03__O_W_LOT_ID_1ST);

                            _EIFServer.SetVarStatusLog(sender, $"ULD Transfer Cell[{i}] Route LOT ID = [{outData.OUTDATA[0].ROUTID_1ST}, {outData.OUTDATA[0].DAY_GR_LOTID_1ST}]");
                        }
                        else
                        {
                            this.BASE.G4_6_CELL_INFO_REQ_03__O_W_ROUTE_ID_2ND = IsRl ? outData.OUTDATA[0].ROUTID_2ND : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_03__O_W_ROUTE_ID_2ND);
                            this.BASE.G4_6_CELL_INFO_REQ_03__O_W_GROUP_LOT_ID_2ND = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID_2ND : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_03__O_W_GROUP_LOT_ID_2ND);
                            //this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_2ND = IsRl ? outData.OUTDATA[0].SPECIALINFO_2ND : _EIFServer.GetSimValue(() => this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_2ND); //LHS 2023.10.13 : 생산팀 요청으로 인한 ULD CELL TRANSFER 특관추가
                            //this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_NO_2ND = IsRl ? outData.OUTDATA[0].FORM_SPCL_GR_ID_2ND : _EIFServer.GetSimValue(() => this.ULD_CELL__O_W_ULD_TRANSFER_CELL_SPECIAL_INFO_NO_2ND); //LHS 2023.10.13 : 생산팀 요청으로 인한 ULD CELL TRANSFER 특관추가
                            this.BASE.G4_6_CELL_INFO_REQ_03__O_W_LOT_ID_2ND = IsRl ? outData.OUTDATA[0].DAY_GR_LOTID_2ND : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ_03__O_W_LOT_ID_2ND);

                            _EIFServer.SetVarStatusLog(sender, $"ULD Transfer Cell[{i}] Route LOT ID = [{outData.OUTDATA[0].ROUTID_2ND}, {outData.OUTDATA[0].DAY_GR_LOTID_2ND}]");
                        }
                    }

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);
                }
                else
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Fail : Tray ID=[{this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                }

                _EIFServer.SetVarStatusLog(sender, "ULD Transfer Cell ID Confirm : [ON]");

            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G4_6_CELL_INFO_REQ_03__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G4_6_CELL_INFO_REQ_03__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion

        #region ULD Tray Transfer(Buffer Out) Complete
        protected virtual void __G2_3_CARR_OUT_RPT_02__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_3_CARR_OUT_RPT_02__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_3_CARR_OUT_RPT_02__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_3_CARR_OUT_RPT_02__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(sender, eventName + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_LOT_ID = "";
                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_GROUP_LOT_ID = "";
                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_ROUTE_ID = "";
                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_SPECIAL_INFO = "";
                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_SPECIAL_NO = "";
                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_LOT_TYPE = "";

                    _EIFServer.SetVarStatusLog(sender, "ULD Tray Transfer Complete Confirm  : [OFF]");
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

                string jigID = IsHR ? this.BASE.G2_3_CARR_OUT_RPT_02__I_W_JIG_ID : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_02__I_W_JIG_ID);
                string trayID = IsHR ? this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID);
                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_3_CARR_OUT_RPT_02__I_W_JIG_ID))} : {jigID}, {GetDesc(nameof(this.BASE.G2_2_CARR_JOB_START_RPT_03__I_W_TRAY_ID))} : {trayID})");

                int iRst = 0;
                string strBizName = "BR_SET_JIG_FORM_ULD_BUFFER_OUT";
                Exception bizEx = null;

                CBR_SET_JIG_FORM_ULD_BUFFER_OUT_IN inData = CBR_SET_JIG_FORM_ULD_BUFFER_OUT_IN.GetNew(this);
                CBR_SET_JIG_FORM_ULD_BUFFER_OUT_OUT outData = CBR_SET_JIG_FORM_ULD_BUFFER_OUT_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = jigID;

                //입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} No Input Data!! : ULD Tray Jig ID=[{jigID}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, inData, outData, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 8);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                //BizRule 정상 처리 후
                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Success : ULD Tray Jig ID=[{jigID}], Tray ID=[{trayID}]");

                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_ROUTE_ID = IsRl ? outData.OUTDATA[0].ROUTID : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_02__O_W_ROUTE_ID);
                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_GROUP_LOT_ID = IsRl ? outData.OUTDATA[0].PROD_LOTID : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_02__O_W_GROUP_LOT_ID);
                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_SPECIAL_INFO = IsRl ? outData.OUTDATA[0].SPECIALINFO : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_02__O_W_SPECIAL_INFO);
                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_SPECIAL_NO = IsRl ? outData.OUTDATA[0].FORM_SPCL_GR_ID : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_02__O_W_SPECIAL_NO); // HDH 2023.10.12 : SPECIALINFO -> FORM_SPCL_GR_ID 수정
                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_LOT_ID = IsRl ? outData.OUTDATA[0].PROD_LOTID : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_02__O_W_LOT_ID);
                    this.BASE.G2_3_CARR_OUT_RPT_02__O_W_LOT_TYPE = IsRl ? outData.OUTDATA[0].LOTTYPE : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_02__O_W_LOT_TYPE);

                    _EIFServer.SetVarStatusLog(sender, $"ULD Tray RouteID : [{this.BASE.G2_3_CARR_OUT_RPT_02__O_W_ROUTE_ID}], GrpLotID : [{this.BASE.G2_3_CARR_OUT_RPT_02__O_W_GROUP_LOT_ID}], SpecialInfo : [{this.BASE.G2_3_CARR_OUT_RPT_02__O_W_SPECIAL_INFO}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);
                }
                else
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} Fail : ULD Tray Jig ID=[{jigID}], Tray ID=[{trayID}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                }

                _EIFServer.SetVarStatusLog(sender, "ULD Tray Transfer Complete Confirm  : [ON]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_3_CARR_OUT_RPT_02__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_3_CARR_OUT_RPT_02__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion

        #region ULD Job Complete
        protected virtual void __G2_3_CARR_OUT_RPT_03__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_3_CARR_OUT_RPT_03__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_3_CARR_OUT_RPT_03__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_3_CARR_OUT_RPT_03__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(sender, eventName + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(sender, "ULD Job Complete Confirm =[OFF]");
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

                if (!this.BASE.G2_3_CARR_OUT_RPT_03__I_B_TRAY_EXIST)
                {
                    _EIFServer.SetVarStatusLog(sender, $"Invalid EQP Status!! ULD Tray Exist : [{(this.BASE.G2_3_CARR_OUT_RPT_03__I_B_TRAY_EXIST ? "EXIST" : "EMPTY")}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                string trayID = IsHR ? this.BASE.G2_3_CARR_OUT_RPT_03__I_W_TRAY_ID.Trim() : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_03__I_W_TRAY_ID);
                string routeID = IsHR ? this.BASE.G2_3_CARR_OUT_RPT_03__I_W_ROUTE_ID.Trim() : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_03__I_W_ROUTE_ID);
                string groutLotID = IsHR ? this.BASE.G2_3_CARR_OUT_RPT_03__I_W_GROUP_LOT_ID.Trim() : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_03__I_W_GROUP_LOT_ID);

                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_3_CARR_OUT_RPT_03__I_W_TRAY_ID))} : {trayID}, {GetDesc(nameof(this.BASE.G2_3_CARR_OUT_RPT_03__I_W_GROUP_LOT_ID))} : {groutLotID}");

                if (IsHR)   //$ 2021.11.29 : 하기 조건은 Host Simul Test가 아닌 경우에만 확인한다.
                {
                    if (string.IsNullOrEmpty(trayID) || string.IsNullOrEmpty(routeID) || string.IsNullOrEmpty(groutLotID))
                    {
                        _EIFServer.SetVarStatusLog(sender, $"Invalid Data!! TrayID : [{trayID}], RouteID : [{routeID}], LotID :[{groutLotID}]");

                        HostAlarm(sender, EIFALMCD.JIGCHKID.ToString(), "Please check the TRAY ID or ROUTE ID or LOT ID", 5);

                        _EIFServer.SetVarStatusLog(sender, $"ULD Station Host Trouble : [ON]");

                        //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                        return;
                    }
                }

                //CELL Count 로그
                _EIFServer.SetVarStatusLog(sender, $"ULD Buffer Cell Count=[{this.BASE.G2_3_CARR_OUT_RPT_03__I_W_CELL_CNT}]");

                List<string> arrCellIDList = this.BASE.G2_3_CARR_OUT_RPT_03__I_W_CELL_ID_LIST; //$ 2024.03.07 : 배열에 대한 값을 Read/Write할때 따로 변수 처리를 해야 부하나 속도 이슈가 없다고 함

                for (int i = 0; i < this.BASE.EQP_INFO__V_TRAYINCELLCNT; i++)    //$ 2021.12.15 : Virtual 변수에서 Cell 전체 수량 받아옴
                {
                    if ("NOREAD".Equals(arrCellIDList[i].Trim())) //CELL ID NOREAD 일 경우 체크
                        _EIFServer.SetVarStatusLog(sender, $"ULD Tray Cell[{i}]'s ID=[{arrCellIDList[i]}] is invalid!!");
                }

                int iRst = 0;
                Exception bizEx = null;
                string strBizName = "BR_SET_JIG_FORM_ULD_TRAY_CREATE";

                CBR_SET_JIG_FORM_ULD_TRAY_CREATE_IN inData = CBR_SET_JIG_FORM_ULD_TRAY_CREATE_IN.GetNew(this);
                CBR_SET_JIG_FORM_ULD_TRAY_CREATE_OUT outData = CBR_SET_JIG_FORM_ULD_TRAY_CREATE_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayID;
                inData.INDATA[inData.INDATA_LENGTH - 1].CELL_COUNT = IsHR ? this.BASE.G2_3_CARR_OUT_RPT_03__I_W_CELL_CNT : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_03__I_W_CELL_CNT);
                inData.INDATA[inData.INDATA_LENGTH - 1].DAY_GR_LOTID = groutLotID;

                inData.CELLDATA_LENGTH = 0;
                for (ushort i = 0; i < (IsHR ? this.BASE.G2_3_CARR_OUT_RPT_03__I_W_CELL_EXIST_LIST.Length : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT_03__I_W_CELL_EXIST_LIST).Length); i++)
                {
                    inData.CELLDATA_LENGTH++;
                    inData.CELLDATA[inData.CELLDATA_LENGTH - 1].CSTSLOT = i + 1;
                    inData.CELLDATA[inData.CELLDATA_LENGTH - 1].SUBLOTID = IsHR ? arrCellIDList[i].Trim() : _EIFServer.GetSimArrValue(() => this.BASE.G2_3_CARR_OUT_RPT_03__I_W_CELL_ID_LIST, i); //$ 2024.03.07 : 기존 Factova 변수에 배열값을 직접 Read하던 부분을 지역변수로 변경했음, SimulData는 PLC와 무관하므로 기존 방식 유지
                }

                //입력 데이터가 없으면
                if (inData.INDATA.Count == 0 || inData.CELLDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName} No Input Data!! : TRAY ID=[{trayID}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, inData, outData, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 5);

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

                _EIFServer.SetVarStatusLog(sender, "ULD Job Complete Confirm =[ON]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_3_CARR_OUT_RPT_03__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_3_CARR_OUT_RPT_03__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion
    }
}