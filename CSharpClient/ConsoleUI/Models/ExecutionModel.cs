using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpClient.ConsoleUI.Models
{
    public class ExecutionModel
    {
        /**
         *   Execution
         */

        public int OrderId { get; set; }

        /**
         * @brief The API client identifier which placed the order which originated this execution.
         */
        public int ClientId { get; set; }

        /**
         * @brief The execution's identifier. Each partial fill has a separate ExecId. 
		 * A correction is indicated by an ExecId which differs from a previous ExecId in only the digits after the final period,
		 * e.g. an ExecId ending in ".02" would be a correction of a previous execution with an ExecId ending in ".01"
         */
        public string ExecId { get; set; }

        /**
         * @brief The execution's server time.
         */
        public string Time { get; set; }

        /**
         * @brief The account to which the order was allocated.
         */
        public string AcctNumber { get; set; }

        /**
         * @brief The exchange where the execution took place.
         */
        public string Exchange { get; set; }

        /**
         * @brief Specifies if the transaction was buy or sale
         * BOT for bought, SLD for sold
         */
        public string Side { get; set; }

        /**
         * @brief The number of shares filled.
         */
        public double Shares { get; set; }

        /**
         * @brief The order's execution price excluding commissions.
         */
        public double Price { get; set; }

        /**
         * @brief The TWS order identifier. The PermId can be 0 for trades originating outside IB. 
         */
        public int PermId { get; set; }

        /**
         * @brief Identifies whether an execution occurred because of an IB-initiated liquidation. 
         */
        public int Liquidation { get; set; }

        /**
         * @brief Cumulative quantity. 
         * Used in regular trades, combo trades and legs of the combo.
         */
        public double CumQty { get; set; }

        /**
         * @brief Average price. 
         * Used in regular trades, combo trades and legs of the combo. Does not include commissions.
         */
        public double AvgPrice { get; set; }

        /**
         * @brief The OrderRef is a user-customizable string that can be set from the API or TWS and will be associated with an order for its lifetime.
         */
        public string OrderRef { get; set; }

        /**
         * @brief The Economic Value Rule name and the respective optional argument.
         * The two values should be separated by a colon. For example, aussieBond:YearsToExpiration=3. When the optional argument is not present, the first value will be followed by a colon.
         */
        public string EvRule { get; set; }

        /**
         * @brief Tells you approximately how much the market value of a contract would change if the price were to change by 1.
         * It cannot be used to get market value by multiplying the price by the approximate multiplier.
         */
        public double EvMultiplier { get; set; }

        /**
         * @brief model code
         */
        public string ModelCode { get; set; }

        /**
         * @brief The liquidity type of the execution. Requires TWS 968+ and API v973.05+. Python API specifically requires API v973.06+.
         */
        public string LastLiquidity { get; set; }


        /**
         *   CommissionReport
         */

        /**
         * @brief the commissions cost.
         */
        public double Commission { get; set; }

        /**
        * @brief the reporting currency.
        */
        public string Currency        {            get; set;        }

        /**
        * @brief the realized profit and loss
        */
        public double RealizedPNL
        {
            get; set;
        }

        /**
         * @brief The income return.
         */
        public double Yield
        {
            get; set;
        }

        /**
         * @brief date expressed in yyyymmdd format.
         */
        public int YieldRedemptionDate
        {
            get; set;
        }

        /**
        *   Contract
        */

        public int ConId
        {
            get; set;
        }


        /**
         * @brief The underlying's asset symbol
         */
        public string Symbol
        {
            get; set;
        }

        /**
         * @brief The security's type:
         *      STK - stock (or ETF)
         *      OPT - option
         *      FUT - future
         *      IND - index
         *      FOP - futures option
         *      CASH - forex pair
         *      BAG - combo
         *      WAR - warrant
         *      BOND- bond
         *      CMDTY- commodity
         *      NEWS- news
         *		FUND- mutual fund
		 */
        public string SecType
        {
            get; set;
        }

        /**
        * @brief The contract's last trading day or contract month (for Options and Futures). Strings with format YYYYMM will be interpreted as the Contract Month whereas YYYYMMDD will be interpreted as Last Trading Day.
        */
        public string LastTradeDateOrContractMonth
        {
            get; set;
        }

        /**
         * @brief The option's strike price
         */
        public double Strike
        {
            get; set;
        }

        /**
         * @brief Either Put or Call (i.e. Options). Valid values are P, PUT, C, CALL. 
         */
        public string Right
        {
            get; set;
        }

        /**
         * @brief The instrument's multiplier (i.e. options, futures).
         */
        public string Multiplier
        {
            get; set;
        }

        /**
         * @brief The contract's symbol within its primary exchange
		 * For options, this will be the OCC symbol
         */
        public string LocalSymbol
        {
            get; set;
        }

        /**
         * @brief The contract's primary exchange.
		 * For smart routed contracts, used to define contract in case of ambiguity. 
		 * Should be defined as native exchange of contract, e.g. ISLAND for MSFT
		 * For exchanges which contain a period in name, will only be part of exchange name prior to period, i.e. ENEXT for ENEXT.BE
         */
        public string PrimaryExch
        {
            get; set;
        }

        /**
         * @brief The trading class name for this contract.
         * Available in TWS contract description window as well. For example, GBL Dec '13 future's trading class is "FGBL"
         */
        public string TradingClass
        {
            get; set;
        }

        /**
        * @brief If set to true, contract details requests and historical data queries can be performed pertaining to expired futures contracts.
        * Expired options or other instrument types are not available.
        */
        public bool IncludeExpired
        {
            get; set;
        }

        /**
         * @brief Security's identifier when querying contract's details or placing orders
         *      ISIN - Example: Apple: US0378331005
         *      CUSIP - Example: Apple: 037833100
         */
        public string SecIdType
        {
            get; set;
        }

        /**
        * @brief Identifier of the security type
        * @sa secIdType
        */
        public string SecId
        {
            get; set;
        }

        /**
        * @brief Description of the combo legs.
        */
        public string ComboLegsDescription
        {
            get; set;
        }

        /**
         * @brief The legs of a combined contract definition
         * @sa ComboLeg
         */
        public string ComboLegs
        {
            get; set;
        }

        /**
         * @brief Delta and underlying price for Delta-Neutral combo orders.
         * Underlying (STK or FUT), delta and underlying price goes into this attribute.
         * @sa DeltaNeutralContract
         */
        public string DeltaNeutralContract
        {
            get; set;
        }

        public override string ToString()
        {
            return Symbol + " " + Side + " " + Shares + " @ " + Price;
        }
    }
}
