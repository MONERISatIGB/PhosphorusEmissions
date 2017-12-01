using System;
using System.Collections.Generic;
using System.Text;
using Moneris.Core.Data;

namespace Moneris.Core.Engine
{
    /// <summary>
    /// Engine to calculate phosphorus emissions 
    /// </summary>
    public class EmissionsPhosphorus : BaseEngineClass, Moneris.Core.Engine.IEmissionsPhosphorus
    {

        /// <summary>
        /// Basics class with information about 
        /// area, water and unspecific emissions, 
        /// sum of discharge, N and P load point sources
        /// </summary>
        private IBasics basics;
        /// <summary>
        /// Baiscs class with information about 
        /// area, water and unspecific emissions
        /// sum of discharge, N and P load point sources
        /// </summary>
        public IBasics Basics
        {
            set { basics = value; }
        }

        /// <summary>
        /// Countries or states in which the analytical units are located
        /// </summary>
        private CountryOrStates countriesOrStates;
        /// <summary>
        /// Countries or states in which the analytical units are located
        /// </summary>
        public CountryOrStates CountriesOrStates
        {
            set
            {
                countriesOrStates = value;
            }
        }


        /// <summary>        
        /// backgroundRetentionFactor = gw_TNConcentration / leakageWaterTNConcentration 
        /// (BG_GW_TNC / BG_LW_TNC)
        /// </summary>
        private double backgroundRetentionFactor;
        /// <summary>        
        /// backgroundRetentionFactor = gw_TNConcentration / leakageWaterTNConcentration 
        /// (BG_GW_TNC / BG_LW_TNC)
        /// </summary>
        public double BackgroundRetentionFactor
        {
            set { backgroundRetentionFactor = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary> 
        public EmissionsPhosphorus()
        {
            
        }

        /// <summary>
        /// GW RETENTION       
        /// (GW_RetentionFactor = GW_TNC_allAreas / GW_TNC_LW_allAreas)
        /// </summary>
        private double gwRetentionFactor;
        /// <summary>
        /// GW RETENTION        
        /// denn GWRetentionFactor wird basierend auf TN Konzentration berechnet:
        /// (GW_RetentionFactor = GW_TNC_allAreas / GW_TNC_LW_allAreas)
        /// </summary>
        public double GWRetentionFactor
        {
            set { gwRetentionFactor = value; }
        }

        /// <summary>
        /// Private field for result text file (US_onlySS_TP)
        /// </summary>
        private double onlySewers_TP;

        /// <summary>
        /// Private field for result text file (US_SS_TP)
        /// </summary>
        private double ss_TP_discharge;

        /// <summary>
        /// Private field for result text file (US_noSS_TP)
        /// </summary>
        private double noSS_TP;
        /// <summary>
        /// Private field for result text file (US_TP_Inh_septic_tank)
        ///Changed30092012 by Markus renamed from tancs to tanks
        /// </summary>
        private double tpInhabitantsSepticTanks = 0;
        /// <summary>
        /// Private field for result text file (US_DCTP_TP_DIN1_direct)
        /// </summary>
        private double dctpTPDIN1Direct = 0;
        /// <summary>
        /// Private field for result text file (US_DCTP_TP_not_DIN_sewers)
        /// </summary>
        private double dctpTPDIN1Sewer;
         /// <summary>
        /// Private field for result text file (US_DCTP_TP_DIN1_groundwater)
        /// </summary>
        private double dctpTPDIN1Groundwater = 0;
        /// <summary>
        /// Private field for result text file (US_DCTP_TP_DIN2_direct)
        /// </summary>
        private double dctpTPDIN2Direct = 0;
        /// <summary>
        /// Private field for result text file (US_DCTP_TP_DIN2_groundwater)
        /// </summary>
        private double dctpTPDIN2Groundwater = 0;
        /// <summary>
        /// Private field for result text file (US_vZKA_TP)
        /// </summary>
        private double virtualWWTP_TP;
        /// <summary>
        /// Calculate emissions phosphorus
        /// </summary>
        /// <param name="result"></param>
        /// <param name="basics"></param>
        public override void Calc(Result result)
        {

            #region General
            AnalyticalUnit analyticalUnit = result.AnalyticalUnit;

            if (analyticalUnit.Area == 0)
                return;

            Soil soil = analyticalUnit.Soil;
            Landuse landUse = analyticalUnit.Landuse;
            Hydrogeology hydroGeology = analyticalUnit.Hydrogeology;
            PointSources pointSources = analyticalUnit.Pointsources;
            PeriodicalData periodicalData = result.PeriodicalData;
            int year = periodicalData.Year;
            if (year >= Constants.LONGTERM) //Länderdaten haben keine Daten für Feucht-, Mittel-, Trockenbedingungen!
                year = countriesOrStates.CalculationYearForHydrologicalConditions;
            CountryOrState country = countriesOrStates.GetCountry(analyticalUnit, year);
            if (country == null)
                throw new OwnException(countriesOrStates.CountryDataNotAvailable + "\n\n" + countriesOrStates.UnableToRunModel);

            Option option = analyticalUnit.Option;
            #endregion

            #region Intermediate
            //RKB_SRW and RBF_FIL of intermediate_phosphorus() are calculated in Basics 
            //because they are needed in EmissionsNitrogen as well
            #endregion

            #region Urban systems

            //Separate Sewers Rainwater collecting TP conc ((US_SSRW_TPC)  
            double rss_TP_concentration = 0;
            double rss_TP_concentration_PavedUrbanAreas = 0;
            double rss_TP_concentration_CommercialAreas = 0;
            
              if (periodicalData.Precipitation_Anual != 0)
            {
                //##Markus: die atmo Dep muss sich auf die US aud nicht AL beziehen
                //PavedArea_Qratio muss sich auf atmo dep und die zusätzliche "Hundekacke" beziehen
                rss_TP_concentration_PavedUrbanAreas = (Constants.CUS10 * 100.0 * basics.UrbanArea_connected_SS / basics.WaterAmount_SS_urbanAreas / 1000 * 1000000.0);
                #region Comments
                //Units:            [mg/l]           =  (  [kg/(ha*a)] * [ha/km²] *         [km²]        /            [m³/a]         / [l/m³] *  [mg/kg]                         
                //This equation gives the mean TP concentration for run off from paved urban areas connected to separate rain water sewers
                #endregion

                rss_TP_concentration_CommercialAreas = Constants.CUS9;
                #region Comments
                //Units:               [mg/l]               =        [mg/l]
                //This equation gives the mean Tn concentration for run off from commercial areas connected to separate rain water sewers
                #endregion

                rss_TP_concentration = (rss_TP_concentration_PavedUrbanAreas * basics.WaterAmount_SS_urbanAreas + rss_TP_concentration_CommercialAreas * basics.WaterAmount_SS_commercialAreas) / (basics.WaterAmount_SS_urbanAreas + basics.WaterAmount_SS_commercialAreas) * (1.0 - Constants.CUS33 * basics.StorageRSS / 100.0) * (1.0 - Constants.CUS35 * basics.RetentionRBF / 100.0);
                #region Comments
                //Units:[mg/l]       = (              [mg/l]                 *              [m³/a]       +               [mg/l]                 *              [m³/a]           ) / (        [m³/a]           +             [m³/a]            ) * ([-] -         [-]     *         [%]       /  [%] ) * ([-] -         [-]     *           [%]       /  [%] )        
                //Gives the mean concentration in rain water pipe of separat sewers under consideration of a retention by sedimentation basins (StorageRSS) and retention soil filters (RBF)
                #endregion

                #region archiv old equation
                //rss_TP_concentration = Constants.CUS10 * 100.0 / (periodicalData.Precipitation_Anual * basics.PavedArea_Qratio)
                //     * (1.0 - Constants.CUS33 * basics.StorageRSS / 100.0) * (1.0 - Constants.CUS35 * basics.RetentionRBF / 100.0);
                #endregion
            }
            
            //TP concentration combined sewer overflow (US_CSO_TPC)
            double temp1 = 0;
            if (!option.PfreeLaundryDetergents)
            {
                if (!option.PFreeDishwaterDetergents)
                    temp1 = country.PhosphorInhabitant;                    
                else
                    temp1 = country.PhosphorInhabitant - country.PhosphorDishwaterDetergent;
            }
            else if (!option.PFreeDishwaterDetergents)
            {
                if (!option.PfreeLaundryDetergents)
                    temp1 = country.PhosphorInhabitant;
                else
                    temp1 = country.PhosphorInhabitant - country.PhosphorLaundryDetergent;
            }
            else
                temp1 = country.PhosphorInhabitant - country.PhosphorLaundryDetergent - country.PhosphorDishwaterDetergent;

            double cso_TP_concentration = 0;// (US_CSO_TPC)
            double CsoInhabitantsTP = 0.0;
            double CsoCommercialAreasTP = 0;
            double CsoPavedAreasTP = 0;

            if (basics.UrbanArea_connected_CSS != 0.0)
            {
                CsoInhabitantsTP = periodicalData.Inhabitants / 1000.0 * basics.InhabitantsConnectedToSewerAndWWTP / basics.InhabitantsConnectedCorrected * basics.UrbanArea_connected_CSS / result.PavedAreaTotal * temp1 * basics.StormWaterEvents_effectiveDays;
                #region Comments
                //Units:[kg/a]   =        [inhabitants]       / [g/kg] *                [inhabitants]              /           [inhabitants]              *                [km²]           /           [km²]   * [g/(inhabitant*d)] *  [d/a]
                //This equation gives the total efluents in combined sewers from inhabitants as sum for all days with effective storm water events 
                #endregion

                CsoCommercialAreasTP = Constants.CUS15 * basics.CommercialArea_connected_CSS * 100.0 * 86400 / 1000.0 * Constants.CUS9 * basics.StormWaterEvents_effectiveDays / 1000000;
                #region Comments
                //Units:  [kg/a]     =  [l/(ha*s)]]    *          [km²]             * [ha/km²] * [s/d] / [l/m³] *     [mg/l]      *           [d/a]                     / [mg/kg]                                                 
                //This equation gives the total efluents from commercial areas, connected to combined sewers as sum for all days with effective storm water events 
                #endregion

                CsoPavedAreasTP = Constants.CUS10 * 100.0 * basics.PavedArea_Qratio * basics.UrbanArea_connected_CSS / 365 * basics.StormWaterEvents_effectiveDays;
                #region Comments
                //Units:     [kg/a]    = (          [kg/(km²*a)]         +          [kg/(km²*a)]          +   [kg/(ha*a)]  * [ha/km²] *          [-]           *            [km²]
                //This equation gives the total loads from paved areas connected to combined sewers as sum for a year (all days not only days with storm water events)
                #endregion]

                cso_TP_concentration = (CsoInhabitantsTP + CsoCommercialAreasTP + CsoPavedAreasTP) / basics.CSO_discharge_duringOverflow / 1000.0 * 1000000.0;
                #region Comments
                //Units    [mg/l]    = (     [kg/a]      +       [kg/a]         +     [kg/a]     ) /              [m³/a]                 / [l/m³] *  [mg/kg]
                //This equation gives the mean concentration in combined sewers considering loads from inhabitants, commercial areas and paved urban areas connected to combined sewer systems
                //For inhabitants and commercial areas only days with effective storm water events are considered, where for paved area the entire load over a year
                //is used. This is done analogously for the respective water amount from these three sources. Like this the mean load from paved areas is combined with the effluents from
                //inhabitants and commercial areas for days with stormwater events.
                #endregion]

                //cso_TP_concentration = (((basics.InhabitantsConnectedToSewerAndWWTP * temp1 * basics.StormWaterEvents_effectiveDays +
                //    Constants.CUS15 * Constants.CUS46 / 100.0 * result.PavedAreaTotal * 100.0 * 365.0 * 86400.0 / 1000.0 / 1000.0 * Constants.CUS9 *
                //    basics.StormWaterEvents_effectiveDays / 365 * Constants.CUS16 / 24.0) * basics.UrbanArea_connected_CSS / result.PavedAreaTotal
                //    + (Constants.CUS10 * basics.UrbanArea_connected_CSS * 100.0))) / basics.CSO_discharge_duringOverflow;
                //##Markus: analog zu TN geändert
            }                   
    
            //P retention no sewers (US_RET_noSS_TP)
            double hydrogeoArea = hydroGeology.ConsolidatedRock_p + hydroGeology.ConsolidatedRock_imp + hydroGeology.UnconsolidatedRock_sg + hydroGeology.UnconsolidatedRock_dg;

            //Changed30092012 by Markus 
            //This retention factor has been replaced by the retention calculated in the groundater pathway, as retention_TN_noSS did not consider retention in soils
            //double retention_TP_noSS = hydrogeoArea;
            //if (retention_TP_noSS != 0.0)
            //    retention_TP_noSS = (Constants.CUS22 * (hydroGeology.ConsolidatedRock_p +
            //        hydroGeology.ConsolidatedRock_imp) + Constants.CUS23 * (hydroGeology.UnconsolidatedRock_sg
            //        + hydroGeology.UnconsolidatedRock_dg)) / hydrogeoArea;

            //separate sewers TP discharge (US_SS_TP)
            result.SS_TP_discharge = rss_TP_concentration * basics.WaterAmount_ss * 1000.0 / 1000000000.0;

            //combined sewer TP discharge (US_CS_TP)
            result.CS_TP_discharge = result.CSO_current_discharge * cso_TP_concentration *1000.0 / 1000000000.0;
           
            //Einträge von Einwohnern, die nur an die Kanalisation angeschlossen sind und von Gewerbeflächen
            //inhabitants TP discharge (US_INH_TP)
            //Achtung temp1 wird bei der Kalkulation von cso_TP_concentration "TP Konz combined sewer overflow" gesetzt!
            double inhabitants_TP_discharge = basics.InhabitantsConnectedOnlyToSewers * temp1 * 365 / 1000000;
 
            //double inhabitants_TP_discharge = basics.InhabitantsConnectedOnlyToSewers * temp1 * 0.365 + Constants.CUS15 * Constants.CUS46 / 100.0 * result.PavedAreaTotal * 100.0 * Constants.CUS9 * 86.4 * 0.365 / 1000.0 * Constants.CUS16 / 24.0;
            #region Comments
            // Units:     [t/a]               =               [inhabitant]          * [g/(inhabitant*d)] * [d/a] / [g/t]      
            //Gives the total annual emissions from inhabitants in housholds only connected to sewers but not to a WWTP
            #endregion

            //impervious areas P discharge 
            double pavedArea_P_discharge = basics.UrbanArea_onlyConnected_SS * Constants.CUS10 *100.0 / 1000.0; //US_IUA_TP

            //Ratio TP entry from detergents per inhabitant and day
            //to TP entry per inhabitant and day(US_Pfree_ratio) 
            double pfree_ratio = 0;
            if (!option.PfreeLaundryDetergents)
            {
                if (!option.PFreeDishwaterDetergents)
                    pfree_ratio = 1.0 / 0.850515;
                else
                    pfree_ratio = (country.PhosphorInhabitant - country.PhosphorDishwaterDetergent) / country.PhosphorInhabitant / 0.850515;
            }
            else if (!option.PFreeDishwaterDetergents)
            {
                if (!option.PfreeLaundryDetergents)
                    pfree_ratio = 1.0 / 0.850515;
                else
                    pfree_ratio = (country.PhosphorInhabitant - country.PhosphorLaundryDetergent) / country.PhosphorInhabitant / 0.850515;
            }
            else
                pfree_ratio = (country.PhosphorInhabitant - country.PhosphorLaundryDetergent - country.PhosphorDishwaterDetergent) / country.PhosphorInhabitant / 0.850515;
                        
            //TP only from sewers not from wwtp    
            //###2.16.015, US_noW_TP soll auch nicht mehr über die individuell WWTP aufsummierung berechnet werden, die
            //Berücksichtigung des SC_PCI Scenarios erfolg nur noch über die Einwohner
            //no WWTP TN, TP und Q aufsummieren erfolgt extern 
            onlySewers_TP = pavedArea_P_discharge;//(US_onlySS_TP)  
            if (!option.AllConnected)
                onlySewers_TP = pavedArea_P_discharge + inhabitants_TP_discharge;


            if (result.GW_Qcorr != 0.0)
                noSS_TP = basics.UrbanArea_notConnected * Constants.CUS10 / 10.0 ;
            //no sewers TP discharge (US_noSS_TP) 
            //Changed300920122 by Markus
            //Is considered as emissions via ground water now and not for urban systems Retention is considered at groundwater calculation
            //If the water balance is negative groundwater recharge is set to 0. In this case also emissions from inhabitants discharging via soil/groundwater are set to 0.




            //***********************************************************************************************
            //DCTP (small WWTP) 
            //***********************************************************************************************

            //Inh TP septic tank t/yr (US_TP_Inh_septic_tank)             
            if (country.PhosphorInhabitant - Constants.CUS19 > 0)
                tpInhabitantsSepticTanks = (100.0 - Constants.CUS36) / 100.0 * basics.InhabitantsConnectedToSepticTanks * (country.PhosphorInhabitant - Constants.CUS19) * 365.0 / 1000000.0;

            //DCTP TP not DIN2 with public sewers t/yr (US_DCTP_TP_not_DIN_sewers) 
            //###2.16.015, US_noW_TP wird nicht > 0 daher rausgenommen
            //DCTP TP nicht DIN2 with public sewers t/yr
            dctpTPDIN1Sewer = (100.0 - Constants.CUS37) / 100.0 * basics.InhabitantsConnectedToDCTPSewerDIN1 * (country.PhosphorInhabitant * pfree_ratio - Constants.CUS19) * 365 / 1000000.0;

            //DCTP TP DIN1 Groundwater t/yr (US_DCTP_TP_DIN1_groundwater)           
            if (country.PhosphorInhabitant > 0)
                if (result.GW_Qcorr != 0.0) 
                                dctpTPDIN1Groundwater = (100.0 - Constants.CUS37) / 100.0 * basics.InhabitantsConnectedToDCTPGroundwaterDIN1 * (country.PhosphorInhabitant * pfree_ratio - Constants.CUS19) * 365 / 1000000.0;
            //Changed24042012 by Markus
            //If the water balance is negative groundwater recharge is set to 0. In this case also emissions from inhabitants discharging via soil/groundwater are set to 0.
            //dctpTPDIN1Groundwater = (100.0 - retention_TP_noSS) / 100.0 * (100.0 - Constants.CUS37) / 100.0 * basics.InhabitantsConnectedToDCTPGroundwaterDIN1 * (country.PhosphorInhabitant * pfree_ratio - Constants.CUS19) * 365 / 1000000.0;
            //Changed30092012 by Markus 
            //This retention factor has been replaced by the retention calculated in the groundater pathway, as retention_TN_noSS did not consider retention in soils

            //country.PhosphorInhabitant = 4.09; //Debug
            //DCTP TP DIN1 ditches and pipes t/yr (US_DCTP_TP_DIN1_direct)              
            if (country.PhosphorInhabitant > 0)
                dctpTPDIN1Direct = (100.0 - Constants.CUS37) / 100.0 * basics.InhabitantsConnectedToDCTPDirectDIN1 * (country.PhosphorInhabitant * pfree_ratio - Constants.CUS19) * 365 / 1000000.0;

            //DCTP TP DIN2 with sewers intermediate calculations
            double inter_CSO = option.CSO_Storage_Increase; //(inter_cso)
            if (inter_CSO == 0)
                inter_CSO = periodicalData.CSO_Storage; // country.CombinedSewerOV;
            double inter_DCTP; //(inter_kka)
            if (option.DIN2withAdditionalPRemoval) //zusätzliche P-Fällung in Kleinkläranlagen
                inter_DCTP = Constants.CUS39; //Zusätzliche P-Fällung in DIN2 DCTPs
            else
                inter_DCTP = Constants.CUS38; //"normale" Reinigungsleistung von DIN2 DCTPs

            //DCTP TP DIN2 with public sewers t/yr (US_DCTP_TP_DIN2_sewers) 
            //'###2.16.015, US_noW_TP wird nicht > 0 daher rausgenommen
            double dctpTPDIN2Sewer = (100.0 - inter_DCTP) / 100.0 * basics.InhabitantsConnectedToDCTPSewerDIN2 * (country.PhosphorInhabitant * pfree_ratio - Constants.CUS19) * 365 /1000000.0;

            //DCTP TP DIN2 Groundwater t/yr (US_DCTP_TP_DIN2_groundwater)             
            if (country.PhosphorInhabitant > 0)
                if (result.GW_Qcorr != 0.0)
                    dctpTPDIN2Groundwater = (100.0 - inter_DCTP) / 100.0 * basics.InhabitantsConnectedToDCTPGroundwaterDIN2 * (country.PhosphorInhabitant * pfree_ratio - Constants.CUS19) * 365 / 1000000.0;
            //Changed24042012 by Markus
            //If the water balance is negative groundwater recharge is set to 0. In this case also emissions from inhabitants discharging via soil/groundwater are set to 0.
            //dctpTPDIN2Groundwater = (100.0 - retention_TP_noSS) / 100.0 * (100.0 - inter_DCTP) / 100.0 * basics.InhabitantsConnectedToDCTPGroundwaterDIN2 * (country.PhosphorInhabitant * pfree_ratio - Constants.CUS19) * 365 / 1000000.0;
            //Changed30092012 by Markus 
            //This retention factor has been replaced by the retention calculated in the groundater pathway, as retention_TN_noSS did not consider retention in soils

            //DCTP TP DIN2 ditches and pipes t/yr (US_DCTP_TP_DIN2_direct)             
            if (country.PhosphorInhabitant > 0)
                dctpTPDIN2Direct = (100.0 - inter_DCTP) / 100.0 * basics.InhabitantsConnectedToDCTPDirectDIN2 * (country.PhosphorInhabitant * pfree_ratio - Constants.CUS19) * 365 / 1000000.0;

            //DCTP transformed to virtual WWTP (ZKA TP virtual t/yr) (US_vZKA_TP) 
            virtualWWTP_TP = basics.InhabitantsConnectedToVirtualWWTP * (country.PhosphorInhabitant - Constants.CUS19) * 365 / 1000000.0;
            if (option.VirtualWWTPwithAdditionalPRemoval)
                virtualWWTP_TP = virtualWWTP_TP * (100.0 - Constants.CUS41) / 100.0; //virtual WWTP with P-removal
            else
                virtualWWTP_TP = virtualWWTP_TP * (100.0 - Constants.CUS40) / 100.0; //virtual WWTP without P-removal

            //Changed05092012 by Markus
            //Tippfehler hier wurde der Variablenname fürStickstoffverwendet
            if (!option.AllConnected) //'diese Werte werden nur noch bei der Retentionsberechnung als Direkteinleiter in den Hauptlauf verwendet              
                pointSources.PEmissionNoWWTP = dctpTPDIN2Sewer + virtualWWTP_TP + dctpTPDIN1Sewer;//###2.16.015, US_IUA_TP durch US_noW_TP ersetzt
            else
                pointSources.PEmissionNoWWTP = 0;//###2.16.015, else-Alternative eingeführt

            //Changed05092012 by Markus 
            //stand weiter oben und wurde hierhin verschoben
            //Checken: wird das bei Szenarien verwendet. Fehlte bei Stickstoof habe ich nun dort eingabuat - der konsitenz wegen.
            //möglicherweise bei n und p wieder raus
            pointSources.PEmissionNoWWTP = pointSources.PEmissionNoWWTP * (1.0 - option.PortionConnectedInhabitants / 100.0);
            
            //Results urban systems

            //Changed30092012 by Markus
            result.DctpTPDIN1DitchOrPipe = dctpTPDIN1Direct;
            result.DctpTPDIN1Groundwater = dctpTPDIN1Groundwater;
            result.OnlySewers_TP = onlySewers_TP;
            result.NoSewerSystem_TP = noSS_TP;
            result.InhabitantsSepticTanks_TP = tpInhabitantsSepticTanks;


            //result.CS_TP_discharge  is considered separately in monthly disagregation 
            //Changed30092012 by Markus: Emisisons via  dctpTPDIN1Groundwater + dctpTPDIN2Groundwater are considered for emisiosn via groundwater
            result.Emission_TP_US = result.SS_TP_discharge + result.CS_TP_discharge + onlySewers_TP + noSS_TP + tpInhabitantsSepticTanks + dctpTPDIN1Direct + dctpTPDIN1Sewer + dctpTPDIN2Direct + dctpTPDIN2Sewer + virtualWWTP_TP;

            result.Emission_TP_US_ForMonthlyDisag_mainlyDrivenByPopulation = inhabitants_TP_discharge + tpInhabitantsSepticTanks + dctpTPDIN1Direct + dctpTPDIN1Sewer + dctpTPDIN2Direct + dctpTPDIN2Sewer + virtualWWTP_TP;

            result.Emission_TP_US_ForMonthlyDisag_mainlyDrivenByPrecipitation = result.SS_TP_discharge + pavedArea_P_discharge + noSS_TP;

            //result.Emission_TP_US_ForMonthlyDisag_dischargingViaGroundwater = dctpTPDIN1Groundwater + dctpTPDIN2Groundwater;



            #endregion

            #region Atmoshperic deposition
            result.Emission_TP_AD = result.WaterSurfaceAreaTotal * periodicalData.Atmo_Dep_TP / 1000.0; //(AD_TP)
            
            #endregion

            #region Surface runoff
            #region Erklärung: Neue Methode für die Korrektur der P-Akkumulation
            //'alte Version:
            //'If BI_Pacc / CSR8 > 1 Then 'Verhältnis von P_akk im Gebiet zum mittleren Wert in Deutschland
            //'    P_acc_de = 1
            //'Else
            //'    P_acc_de = CD_Pacc_coun / CSR8
            //'End If
            //'Problem: falls der BI_P Wert zu groß war, wurde der Faktor auf 1 gesetzt, wenn nicht, wurde der
            //'Country data Werte verwendet. Der dann berechnete Faktor konnte allerding größer 1 werden und
            //'für enorm hohe Sättigungsgrade sorgen, was wiederum für sehr hohe P konzentrationen führte.
            //'Darüber hinaus wurde der räumlich diferenzierte P-Wert der BI nicht berücksichtigt, wodurch eine
            //'räumliche Verteilung der Pacc nicht abgebildet werden konnte.
            //'Neu es wird der BI_P Wert über das Verhältnis der CD_P werts für das Berechnungsjahr zur P_max konstante
            //'korrigiert (ähnlich wie beim N-surplus). Nun wird das Verhältnis aus BI_P_korr Wert und P_konst
            //'gebildet, und somit der Sättigungsgrad möglicherweise reduziert. Sollte der Sättigungsgrad mehr als 97 %
            //'betragen (entspricht eine P-Konz-SR von 6,8 mg/l), dann wir der korrekturfaktor so reduziert, das die
            //'maximale Sättigung von 97 % nicht überschritten wird.
            //'Voraussetzung: der P_konst Wert sollte aus der Counrty data abgeleitet werden und dort dem max. Wert entsprechen
            //' aus allen Länder entsprechen. Der Sättigungsgrad muss dan entsprechent dem P_konst angepasst werden.
            //'Bisher akkumulation von 1100 kg/ga = 90 % auf Ackerland. für Grünland 80 %.
            #endregion

            #region archive old version (first correction after orginal version by Behrendt)
            //P_sat_max = maximal zulässige Sättigung, da sonst unrealistische 
            //P Konzentrationen ausgrechnet werden (P_sat_max)
            double p_sat_max = 97;
            //P_acc_corr ist ehemals P_acc_de (P_acc_corr)

            int countryYear;


            //Changed30092012 by Markus es wurde bei Szenarien immer der gleiche P akku wert verwendet
            country = null;

            if (periodicalData.Year >= Constants.LONGTERM)
                countryYear = countriesOrStates.CalculationYearForHydrologicalConditions; //Changed31052013 by Steffi: Hier muss das Referenzjahr für die Länder verwendet werden und nicht das für N Surplus!
            else
                countryYear = year;

            country = countriesOrStates.GetCountry(analyticalUnit, countryYear);

            double p_acc_corr = country.PhosphorAccum / Constants.CSR8 * analyticalUnit.Soil.PhosphorAccumulation;

            double p_sat_corr_factor_preliminary = p_acc_corr / Constants.CSR8;//(P_sat_corr_factor_preliminary)
            double p_sat_corr_factor_AL; //(P_sat_corr_factor_AL)
            if (p_sat_corr_factor_preliminary * Constants.CSR6 > p_sat_max)
                p_sat_corr_factor_AL = p_sat_max / Constants.CSR6;
            else
                p_sat_corr_factor_AL = p_acc_corr / Constants.CSR8;

            double p_sat_corr_factor_GL; //(P_sat_corr_factor_GL)
            if (p_sat_corr_factor_preliminary * Constants.CSR7 > p_sat_max)
                p_sat_corr_factor_GL = p_sat_max / Constants.CSR7;
            else
                p_sat_corr_factor_GL = p_acc_corr / Constants.CSR8;


            //TP-Fracht Oberflächenabfluss von schneebedeckten Flächen((P_SR_SN)           

            //TP-Konzentration im Oberflächenabfluss von landwirtschaftlichen Nutzflächen(P_SR_conc_AL)
            double pConcentrationArableLand = Constants.CSR9 + Constants.CSR10 * Math.Exp(p_sat_corr_factor_AL * Constants.CSR6 / Constants.CSR11);
            //TP-Konzentration im Oberflächenabfluss von Grünland(P_SR_conc_GL)
            double pConcentrationGrassland = Constants.CSR9 + Constants.CSR10 * Math.Exp(p_sat_corr_factor_GL * Constants.CSR7 / Constants.CSR11);

            //TP-Konzentration im Oberflächenabfluss von vegetationsbedeckten Flächen(P_SR_conc_NatCov)
            double pConcentrationNaturalCovered = 0.00000001;
            if (landUse.NaturalCoveredArea != 0)
                pConcentrationNaturalCovered = Constants.CSR1; //

            double pConcentrationOpenArea = 0.00000001; //(P_SR_conc_OA)
            if (landUse.OpenArea != 0)
                pConcentrationOpenArea = Constants.CSR2; //

            double pConcentrationOpenPitMine = 0.00000001; //(P_SR_conc_OPM)
            if (landUse.OpenPitMine != 0)
                pConcentrationOpenPitMine = Constants.CSR2; //

            double pConcentrationWetland = 0.00000001; //(P_SR_conc_Wetland)
            if (landUse.Wetland != 0)
                pConcentrationWetland = Constants.CSR2; //

            result.TPSurfaceRunoffArableLand = basics.QArableLand * pConcentrationArableLand * 86.4 * 0.365; //(P_SR_AL) 
            result.TPSurfaceRunoffGrassland = basics.QGrassland * pConcentrationGrassland * 86.4 * 0.365; //(P_SR_GL) 
            result.TPSurfaceRunoffNaturalCovered = basics.QNaturalCovered * pConcentrationNaturalCovered * 86.4 * 0.365; //(P_SR_NatCov) 
            result.TPSurfaceRunoffSnow = result.Q_Snow * Constants.CSR12 * 86.4 * 0.365; //(P_SR_Snow) 
            result.TPSurfaceRunoffOpenArea = basics.QOpenArea * pConcentrationOpenArea * 86.4 * 0.365; //(P_SR_OA) 
            double pOpenPitMine = basics.QOpenPitMine * pConcentrationOpenPitMine * 86.4 * 0.365; //(P_SR_OPM) 
            double pWetland = basics.QWetland * pConcentrationWetland * 86.4 * 0.365; //(P_SR_Wetland) 

            //Gesamteinträge über Oberflächenabfluss TP(P_SR)
            result.Emission_TP_SR = result.TPSurfaceRunoffArableLand + result.TPSurfaceRunoffGrassland + result.TPSurfaceRunoffNaturalCovered + result.TPSurfaceRunoffSnow + result.TPSurfaceRunoffOpenArea + pOpenPitMine + pWetland;

            //TP im Oberflächenabfluss von natürlichen Flächen mit Vegetation(P_SR_nsv)
            result.P_NaturalAreas_WithVegetation = result.TPSurfaceRunoffArableLand + result.TPSurfaceRunoffGrassland + result.TPSurfaceRunoffNaturalCovered;
            #endregion

            #region new version on basis of soil density for different soil types
            ////P_sat_max = maximal zulässige Sättigung, da sonst unrealistische 
            ////P Konzentrationen ausgrechnet werden (P_sat_max)
            //double p_sat_max = 97;

            //int endYear;

            ////Changed 11082013 by Markus for Weser Proeject

            //country = null;

            //#region Correct P accumulation
            //#region Comments
            ////MONERIS considers detailed P-accumulation for a reference year. These data may be derived on a state or smaller administrative level.
            ////The values of this reference year are corrected for the specific calculation year on basis of the general change in P-accumulation on a
            ////country level. The reference year is taken from N-surplus data.
            //#endregion

            ////P accumulation for calculation year taken from country data
            //if (periodicalData.Year >= Constants.LONGTERM)
            //    endYear = countriesOrStates.CalculationYearForHydrologicalConditions; //30082013 changed by Steffi - has to be confirmed by Markus
            //else
            //    endYear = year;

            //country = countriesOrStates.GetCountry(analyticalUnit, endYear);
            //double p_acc_calculationYear = country.PhosphorAccum;


            ////P accumulation for reference year taken from country data
            //country = countriesOrStates.GetCountry(analyticalUnit, countriesOrStates.ReferenceYearNSurplus); //30082013 changed by Steffi - has to be confirmed by Markus

            //double p_acc_referenceYear = country.PhosphorAccum;

            ////correction of P-accumulation of detailed P accumulation
            //double p_acc_corr = analyticalUnit.Soil.PhosphorAccumulation;
            //if (p_acc_referenceYear != 0.0)
            //    p_acc_corr = p_acc_calculationYear / p_acc_referenceYear * analyticalUnit.Soil.PhosphorAccumulation;


            ////sandy soils
            //double VolumeWeightSandySoils = 1.1 * 100 * 100 * 30 * 10000 / 1000;  // in kg/ha

            ////spefific weight of sandy soils = 1.1 g/cm³
            ////top soil depth = 30 cm

            ////loamy and silty soils
            //double VolumeWeightLoamySiltySoils = 1.5 * 100 * 100 * 30 * 10000 / 1000;  // in kg/ha
            ////spefific weight of loams silty soils = 1.5 g/cm³
            ////top soil depth = 30 cm

            ////organic soils
            //double VolumeWeightOrganicSoils = 1.3 * 100 * 100 * 30 * 10000 / 1000;  // in kg/ha
            ////spefific weight of organic soils = 1.3 g/cm³
            ////top soil depth = 30 cm


            ////P content sandy soils
            //double PContentSandySoils = p_acc_corr / VolumeWeightSandySoils * 1000000;  // in mg/kg


            ////P content loma and silty soils
            //double PContentLoamySiltySoils = p_acc_corr / VolumeWeightLoamySiltySoils * 1000000;  // in mg/kg


            ////P content organic soils
            //double PContentOrganicSoils = p_acc_corr / VolumeWeightOrganicSoils * 1000000;  // in mg/kg


            //#region Estimate of saturation
            //#region Comments:
            ////Pöthig et al 2010 found following P content in different soils at a saturation of 70-80%
            ////sandy = 260 mg/kg
            ////loamy = 500 mg/kg
            ////decomposed peat = 1320 mg/kg
            ////additionally to these values the max TP content was taken from Fig 5. and equaled with a saturation derived from equation in Fig 4.,
            ////the correlation between content and saturation was assumed to be linear
            //// for arable land the P-accumulation was increased and for grassland decreased by 10 % 
            //#endregion

            //double PSaturationSandyGrassland = PContentSandySoils * 0.9 * 0.0175 + 70.439;
            //double PSaturationSandyArableland = (PContentSandySoils * 1.1 * 0.0175 + 70.439);

            //double PSaturationLoamySityGrassland = PContentLoamySiltySoils * 0.9 * 0.0093 + 72.57;
            //double PSaturationLoamySityArableland = (PContentLoamySiltySoils * 1.1 * 0.0093 + 72.57);

            //double PSaturationOrganicGrassland = PContentLoamySiltySoils * 0.9 * 0.008 + 64.468;
            //double PSaturationOrganicArableland = (PContentLoamySiltySoils * 1.1 * 0.008 + 64.468);

            //double PSaturationGrassland = 40.0;
            //double PSaturationArableland = 50.0;

            ////Calculation of area weighted mean of saturation
            //if (soil.SandySoil + soil.SiltySoil + soil.LoamySoil + landUse.BogDegraded + landUse.FenDegraded != 0.0)
            //{
            //    PSaturationGrassland = (PSaturationSandyGrassland * soil.SandySoil + PSaturationLoamySityGrassland * (soil.SiltySoil + soil.LoamySoil) + PSaturationOrganicGrassland * (landUse.BogDegraded + landUse.FenDegraded)) / (soil.SandySoil + soil.SiltySoil + soil.LoamySoil + landUse.BogDegraded + landUse.FenDegraded);
            //    PSaturationArableland = (PSaturationSandyArableland * soil.SandySoil + PSaturationLoamySityArableland * (soil.SiltySoil + soil.LoamySoil) + PSaturationOrganicArableland * (landUse.BogDegraded + landUse.FenDegraded)) / (soil.SandySoil + soil.SiltySoil + soil.LoamySoil + landUse.BogDegraded + landUse.FenDegraded);
            //}

            ////Checking for too high Saturation values
            //if (PSaturationArableland > p_sat_max)
            //    PSaturationArableland = p_sat_max;

            //if (PSaturationGrassland > p_sat_max)
            //    PSaturationGrassland = p_sat_max;
            //#endregion

            //#endregion

            ////TP-Fracht Oberflächenabfluss von schneebedeckten Flächen((P_SR_SN)           

            ////TP-Konzentration im Oberflächenabfluss von landwirtschaftlichen Nutzflächen(P_SR_conc_AL)
            //double pConcentrationArableLand = Constants.CSR9 + Constants.CSR10 * Math.Exp(PSaturationArableland / Constants.CSR11);

            ////TP-Konzentration im Oberflächenabfluss von Grünland(P_SR_conc_GL)
            //double pConcentrationGrassland = Constants.CSR9 + Constants.CSR10 * Math.Exp(PSaturationGrassland / Constants.CSR11);

            ////TP-Konzentration im Oberflächenabfluss von vegetationsbedeckten Flächen(P_SR_conc_NatCov)
            //double pConcentrationNaturalCovered = 0.00000001;
            //if (landUse.NaturalCoveredArea != 0)
            //    pConcentrationNaturalCovered = Constants.CSR1; //

            //double pConcentrationOpenArea = 0.00000001; //(P_SR_conc_OA)
            //if (landUse.OpenArea != 0)
            //    pConcentrationOpenArea = Constants.CSR2; //

            //double pConcentrationOpenPitMine = 0.00000001; //(P_SR_conc_OPM)
            //if (landUse.OpenPitMine != 0)
            //    pConcentrationOpenPitMine = Constants.CSR2; //

            //double pConcentrationWetland = 0.00000001; //(P_SR_conc_Wetland)
            //if (landUse.Wetland != 0)
            //    pConcentrationWetland = Constants.CSR2; //

            //result.TPSurfaceRunoffArableLand = basics.QArableLand * pConcentrationArableLand * 86.4 * 0.365; //(P_SR_AL) 

            //result.TPSurfaceRunoffGrassland = basics.QGrassland * pConcentrationGrassland * 86.4 * 0.365; //(P_SR_GL) 

            //result.TPSurfaceRunoffNaturalCovered = basics.QNaturalCovered * pConcentrationNaturalCovered * 86.4 * 0.365; //(P_SR_NatCov) 

            //result.TPSurfaceRunoffSnow = result.Q_Snow * Constants.CSR12 * 86.4 * 0.365; //(P_SR_Snow) 

            //result.TPSurfaceRunoffOpenArea = basics.QOpenArea * pConcentrationOpenArea * 86.4 * 0.365; //(P_SR_OA) 

            //double pOpenPitMine = basics.QOpenPitMine * pConcentrationOpenPitMine * 86.4 * 0.365; //(P_SR_OPM) 

            //double pWetland = basics.QWetland * pConcentrationWetland * 86.4 * 0.365; //(P_SR_Wetland) 

            ////Gesamteinträge über Oberflächenabfluss TP(P_SR)
            //result.Emission_TP_SR = result.TPSurfaceRunoffArableLand + result.TPSurfaceRunoffGrassland + result.TPSurfaceRunoffNaturalCovered + result.TPSurfaceRunoffSnow + result.TPSurfaceRunoffOpenArea + pOpenPitMine + pWetland;

            ////TP im Oberflächenabfluss von natürlichen Flächen mit Vegetation(P_SR_nsv)            
            //result.P_NaturalAreas_WithVegetation = result.TPSurfaceRunoffArableLand + result.TPSurfaceRunoffGrassland + result.TPSurfaceRunoffNaturalCovered;

            #endregion
            #endregion

            #region Tile drainage
            //TP conc in drainage
            result.TPConcentrationTileDrainage = 0.000001;
            double soilArea = soil.SandySoil + soil.ClayeySoil + soil.LoamySoil + landUse.FenDegraded + landUse.BogDegraded + soil.SiltySoil; 
            if (soilArea != 0)
                result.TPConcentrationTileDrainage = (Constants.CTD3 * soil.SandySoil + Constants.CTD4 * (soil.ClayeySoil + soil.LoamySoil
                 + soil.SiltySoil) + Constants.CTD5 * landUse.FenDegraded + Constants.CTD6 * landUse.BogDegraded) / soilArea;
            
            //Retention Pond TP Retention    
            double pondRetentionTP_ArableLand = 1.0; //Retention Pond TN (TD_pond_ret_TN_AL)
            if (basics.PondHL_ArableLand != 0)
                pondRetentionTP_ArableLand = 1.0 / (1.0 + Constants.CR3 * Math.Pow(basics.PondHL_ArableLand, Constants.CR4));

            //Retention Pond TP concentration mg/l
            double pondConcentrationTP_ArableLand = result.TPConcentrationTileDrainage * pondRetentionTP_ArableLand; // (TD_pond_conc_TP_AL)


            result.Emission_TP_TD_ArableLand = pondConcentrationTP_ArableLand * result.TD_Q_ArableLand * 86.4 * 0.365;
            result.Emission_TP_TD_Grassland = result.TPConcentrationTileDrainage * result.TD_Q_Grassland * 86.4 * 0.365;

            //TP Eintrag aus Drainagen (TD_TP) 
            result.Emission_TP_TD = result.Emission_TP_TD_ArableLand + result.Emission_TP_TD_Grassland;


            #endregion

            #region Groundwater
            //P CONCENTRATION groundwater     mg/l (GW_TPC)
            double tpConcentration;
            double tpConcentrationArableAndGrassland;
            double rockArea = hydroGeology.UnconsolidatedRock_dg_share + hydroGeology.UnconsolidatedRock_sg_share + hydroGeology.ConsolidatedRock_p_share + hydroGeology.ConsolidatedRock_imp_share;
            soilArea = soil.SandySoil + soil.ClayeySoil + soil.LoamySoil + soil.Fen + soil.Bog + soil.SiltySoil;
            double tpConcentrationWetLand = 0; //(GW_TPC_WL) 
            double tpConcentrationNaturalCovered = 0; //(GW_TPC_NatCov) 

            if(analyticalUnit.ID == 60011)
                tpConcentrationNaturalCovered = 0;

            // Changed20092012 by Markus: um || result.GW_Recharge_A1 == 0 erweitert
            if (soilArea == 0 || rockArea == 0 || result.GW_Recharge_A1 == 0)
            {
                tpConcentration = 0;
                tpConcentrationArableAndGrassland = 0;
            }
            else //P CONCENTRATION area weighted 
            {
                tpConcentration = ((((Constants.CGW4 * soil.SandySoil) + (Constants.CGW5 * (soil.ClayeySoil + soil.LoamySoil + soil.SiltySoil))
                 + (Constants.CGW6 * landUse.FenDegraded) + (Constants.CGW11 * landUse.FenNatural) + (Constants.CGW7 * landUse.BogDegraded) + (Constants.CGW12 * landUse.BogNatural))
                 / soilArea * (result.GW_Recharge_A1 - basics.Gw_Recharge_A2) + Constants.CGW3 * basics.Gw_Recharge_A2) / result.GW_Recharge_A1);

                tpConcentrationArableAndGrassland = ((Constants.CGW4 * soil.SandySoil) + (Constants.CGW5 * (soil.ClayeySoil + soil.LoamySoil + soil.SiltySoil))
                 + (Constants.CGW6 * landUse.FenDegraded) + (Constants.CGW7 * landUse.BogDegraded))
                 / (soil.SandySoil + soil.ClayeySoil + soil.LoamySoil + landUse.FenDegraded + landUse.BogDegraded + soil.SiltySoil);

                if ((landUse.FenDegraded + landUse.FenNatural) == 0)
                {
                    if (landUse.Wetland != 0)
                        tpConcentrationWetLand = tpConcentration;
                }
                else
                    tpConcentrationWetLand = ((Constants.CGW6 * landUse.FenDegraded) + (Constants.CGW11 * landUse.FenNatural) + (Constants.CGW7 * landUse.BogDegraded) + (Constants.CGW12 * landUse.BogNatural)) /
                        (landUse.FenDegraded + landUse.FenNatural + landUse.BogDegraded + landUse.BogNatural);

                tpConcentrationNaturalCovered = Constants.CGW3;
            }

  
            //P CONCENTRATION corrected by redox-factor       mg/l  
            double tpCorrectedByRedoxFactor;//(GW_TPCcorr)
            double tpArableAndGrasslandCorrectedByRedoxFactor; //(GW_TPC_AL_GL_corr) 
            double tpWetlandCorrectedByRedoxFactor;//(GW_TPC_WL_corr) 
            double tpNaturalCoveredCorrectedByRedoxFactor;//(GW_TPC_NatCov_corr) 
            if (gwRetentionFactor < Constants.CGW31) 
            {
                tpCorrectedByRedoxFactor = tpConcentration * Constants.CGW2;
                tpArableAndGrasslandCorrectedByRedoxFactor = tpConcentrationArableAndGrassland * Constants.CGW2;
                tpWetlandCorrectedByRedoxFactor = tpConcentrationWetLand * Constants.CGW2;
                tpNaturalCoveredCorrectedByRedoxFactor = tpConcentrationNaturalCovered * Constants.CGW2;
            }
            else
            {
                tpCorrectedByRedoxFactor = tpConcentration * Constants.CGW1;
                tpArableAndGrasslandCorrectedByRedoxFactor = tpConcentrationArableAndGrassland * Constants.CGW1;
                tpWetlandCorrectedByRedoxFactor = tpConcentrationWetLand * Constants.CGW1;
                tpNaturalCoveredCorrectedByRedoxFactor = tpConcentrationNaturalCovered * Constants.CGW1;
            }

            //P Fracht aus der Wurzelzone ins Grundwasser. Berechnet aus der Diffenerz der
            //Konzentration in Drainagen und der Konzentartionen in das Grundwasser
            //(= retention/aufnahme in aeroben zone + P konzentration nach berücksichtigung des Redox potentials
            //P LOAD  Root zone              t/yr
            result.TPLoad_RootZone = (result.TPConcentrationTileDrainage - tpConcentration + tpCorrectedByRedoxFactor) * result.GW_Q * 86.4 * 0.365; // (GW_RZ_TP)
            result.DischargeRootZone = (landUse.AgriculturalLand - result.DrainedArea) / analyticalUnit.Area * result.GW_Q;  //(GW_RZ_Q)

            //Total Phosphorus
            result.TPGroundwaterArableLand = tpArableAndGrasslandCorrectedByRedoxFactor * result.GW_Qcorr * (result.ArableLand - result.TileDrainedArableLand) / 1000.0; //(GW_TP_AL) 
            result.TPGroundwaterGrassland = tpArableAndGrasslandCorrectedByRedoxFactor * result.GW_Qcorr * (result.GrassLand - result.TileDrainedGrassland) / 1000.0; //(GW_TP_GL) 
            result.TPGroundwaterNaturalCovered = tpNaturalCoveredCorrectedByRedoxFactor * result.GW_Qcorr * landUse.NaturalCoveredArea / 1000.0; //(GW_TP_NatCov) 
            result.TPGroundwaterWetland = tpWetlandCorrectedByRedoxFactor * result.GW_Qcorr * landUse.Wetland / 1000.0; //(GW_TP_Wetland) 

            //Changed 30092012 by Markus dctpTPDIN1Groundwater + dctpTPDIN2Groundwater + noSS_TP is added here. 
            if (landUse.UrbanArea - result.PavedAreaTotalLongTerm < 0) //Agreed modification to VBA 16.014
                result.TPGroundwaterUrban = (dctpTPDIN1Groundwater + dctpTPDIN2Groundwater + noSS_TP) * gwRetentionFactor;
            else
                result.TPGroundwaterUrban = tpNaturalCoveredCorrectedByRedoxFactor * result.GW_Qcorr * (landUse.UrbanArea - result.PavedAreaTotalLongTerm) / 1000.0 + (dctpTPDIN1Groundwater + dctpTPDIN2Groundwater + noSS_TP) * gwRetentionFactor; //(GW_TP_Urban) 
            
            result.TPGroundwaterOpenArea = tpNaturalCoveredCorrectedByRedoxFactor * result.GW_Qcorr * landUse.OpenArea / 1000.0; //(GW_TP_OpenArea) 
            result.TPGroundwaterOpenPitMine = tpNaturalCoveredCorrectedByRedoxFactor * result.GW_Qcorr * landUse.OpenPitMine / 1000.0; //(GW_TP_OpenPitMine)  //Agreed modification to VBA 16.014 (in VBA GW_TP_OpenPitMine is not calculated but used for calculation of GW_TP)
            result.TPGroundwaterSnow = tpNaturalCoveredCorrectedByRedoxFactor * result.GW_Qcorr * landUse.Snow / 1000.0; //(GW_TP_Snow) //Agreed modification to VBA 16.014 (in VBA  GW_TP_Snow is not calculated but used for calculation of GW_TP)
           
            result.Emission_TP_GW = result.TPGroundwaterArableLand + result.TPGroundwaterGrassland + result.TPGroundwaterNaturalCovered
                + result.TPGroundwaterWetland + result.TPGroundwaterUrban + result.TPGroundwaterOpenArea +
                result.TPGroundwaterOpenPitMine + result.TPGroundwaterSnow; //(GW_TP) 

            //Changed20092012
            if (result.GW_Q != 0)
            result.TPConcentrationAllAreas = result.Emission_TP_GW / result.GW_Q * 1000000.0 / 86400.0 / 365.0;
            
            #endregion

            #region Background 
            //ATMO DEP    TP (BG_AD_TP)
            double backgroundAtmoDepositionTP = result.WaterSurfaceAreaTotal * periodicalData.Precipitation_Anual / 1000.0 / 86.4 /
             0.365 * Constants.CBG7;

            //Groundwater TP (BG_GW_TP)
            double backgroundGroundwaterTP = basics.LeakageWaterRate * Constants.CBG6 * basics.GWRechargeArea / 1000.0;
            if (backgroundRetentionFactor <= 0.05) 
                backgroundGroundwaterTP = backgroundGroundwaterTP * 1.5;
            if (backgroundGroundwaterTP > result.Emission_TP_GW)
                backgroundGroundwaterTP = result.Emission_TP_GW;

            //surface runoff TP (BG_SR_TP)
            double backgroundSurfaceRunoffTP = Constants.CBG8 * 86.4 * 0.365 * result.Q_NaturalAreas_withVegetation;

            //Erosion total TP (BG_ER_TP)
            double backgroundErosionTP = Constants.CE13 / 1000000 * basics.EnrichmentRatio * (basics.SoilLossBackground * landUse.ErosionPotentialArea * 100.0 * basics.PrecipitationCorr * Constants.CE15 * basics.BackgroundSedimentDeliveryRatio / 100 + Constants.CBG17 * landUse.Snow * 100.0);
            #region Comments:
            //Units:     [t/a]         =        [mg/kg]  /  [mg/kg]   *         [-]         * (     [t/(ha*a)]   *            [km²]              * [ha/km²] *       [-]              *        [-]      *            [%]                        / [%] +   [t/(ha*a)]   *     [km²]  * [ha/km²])   
            #endregion


            result.Emission_TP_BG = result.TPSurfaceRunoffSnow + backgroundGroundwaterTP + backgroundSurfaceRunoffTP + backgroundAtmoDepositionTP + backgroundErosionTP;//Agreed modification to VBA 16.014 (in VBA P_SR_SN is used instead of P_SR_SNOW)
            #endregion
            
            #region Erosion

            //P content top soil (ER_TS_TPcont)            
            double pContentTopSoil = soil.CorrectPhosphorContent(country.PhosphorAccum);//CD_Pcont, ER_TS_TPcont
            result.TPErosionArableLand = pContentTopSoil / 1000000.0 * basics.SedimentInputArableLand * basics.EnrichmentRatio; //(ER_TP_AL) 
            result.TPErosionGrassland = pContentTopSoil / 1000000.0 * basics.SedimentInputGrassland * basics.EnrichmentRatio; //(ER_TP_GL) 
            result.TPErosionNaturalCovered = basics.SoilLossNaturalCovered * Constants.CE13 * basics.EnrichmentRatio * basics.SedimentDeliveryRatio / 100.0 / 1000000.0; //(ER_TP_NatCov) 
            
            result.TPErosionSnow = basics.SoilLossSnow * Constants.CE13 / 1000000.0; //(ER_TP_Snow) 
            result.Emission_TP_ER = result.TPErosionArableLand + result.TPErosionGrassland + result.TPErosionNaturalCovered + result.TPErosionSnow;//(ER_TP) 

            
            #endregion

            #region Point sources (VBA sub calculate_wwtp)
            //Apply management alternative 
            double interP = onlySewers_TP;
            if (!option.AllConnected)
                interP = 0;
            //Add emissions from periodical data
            //Changed30092012 by Markus Constants.CUS38 "/100.0"
             pointSources.PEmission =  pointSources.PEmissionWWTP * periodicalData.WWTP_P_History + (periodicalData.IndustryDirect_P +
                periodicalData.WWTP_P_Remain) * (1.0 - (option.ReductionP / 100.0)) / 1000.0
                + interP * (1.0 - Constants.CUS38 / 100.0);

             

            #endregion


            #region Final calculation
             pointSources.PEmission =  pointSources.PEmission + virtualWWTP_TP;
            result.Emission_TP_PointSources =  pointSources.PEmission;
            

            //Einträge durch Punktquellen in die Hauptläufe (PS_in_MR_TP)           
            result.Emission_TP_PSinMR = 0;
            if (analyticalUnit.Emission_PS_DirectInMR) //Kläranlagen leiten in den Hauptlauf
               result.Emission_TP_PSinMR =  pointSources.PEmission + result.CS_TP_discharge + pointSources.PEmissionNoWWTP;

            //##Markuens  test auf negative Werte  für 9999 Elbe testdatensatz
            //kann später wieder gelöscht werd
            if (analyticalUnit.ID == 50212)     //50204, 50206, 50207, 50209, 50212,  50222
                periodicalData.Year = periodicalData.Year; 


            result.TPTotalArableLand = result.Emission_TP_TD_ArableLand + result.TPErosionArableLand + result.TPSurfaceRunoffArableLand + result.TPGroundwaterArableLand; //(TP_tot_AL) 

            result.TPTotalGrassland = result.Emission_TP_TD_Grassland + result.TPErosionGrassland + result.TPSurfaceRunoffGrassland + result.TPGroundwaterGrassland; //(TP_tot_AL) 

            result.TPTotalForest = result.TPErosionNaturalCovered + result.TPSurfaceRunoffNaturalCovered + result.TPGroundwaterNaturalCovered;

            result.TPTotalWetland = result.TPGroundwaterWetland + pWetland;

            result.TPTotalUrban = result.Emission_TP_US + result.Emission_TP_PointSources + result.TPGroundwaterUrban;

            result.TPTotalSurfaceWater = result.Emission_TP_AD;

            result.TPTotalOpenPitMine = result.TPGroundwaterOpenPitMine;

            result.TPTotalOpenArea = result.TPErosionSnow + result.TPSurfaceRunoffSnow + result.TPSurfaceRunoffOpenArea + result.TPGroundwaterOpenArea + result.TPGroundwaterSnow;


            // Sum results    
            result.Emission_TP_Total = result.Emission_TP_AD + result.Emission_TP_SR + result.Emission_TP_TD + result.Emission_TP_ER + result.Emission_TP_GW
             +  pointSources.PEmission + result.Emission_TP_US;
            result.PLossRootZone = result.TPLoad_RootZone + result.Emission_TP_TD;
            result.DischargeWWTP = analyticalUnit.Pointsources.DischargeWWTP;
            if (periodicalData.Inhabitants > 0)
                result.TPEmissionPerInhabitant = result.Emission_TP_Total / periodicalData.Inhabitants * 1000000.0;
            #endregion

            #region Sources
            //##Markus: Der gesamte Bereich der Sources wurde überarbeitet. Die Bilanzierung war nicht ganz sauber, da die gesamten Background Einträge 
            //von den landiwrtschaftlichen einträgen abgezogen wurden. Darüber hinaus wurde für BG-Erosionseinträge kein SDR und kein ER angenommen. Es 
            //wird weiterhin der C-Faktor von 0,005 verwendet. 

            //Atmosphaeric deposition TP on water surface areas 
            double sourceAtmoDep = 0.0;//(AD_TP_OS)
            if (result.Emission_TP_AD > backgroundAtmoDepositionTP)
                sourceAtmoDep = result.Emission_TP_AD - backgroundAtmoDepositionTP;

            //Erosion
            double sourceErosion = Constants.CE13 / 1000000.0 * basics.SoilLossNatural; //(ER_TP_OS)
            if (sourceErosion > backgroundErosionTP)
            {
                sourceErosion = result.Emission_TP_ER - backgroundErosionTP;

                //  sourceErosion = sourceErosion - backgroundErosionTP;
            }
            else
                sourceErosion = 0;

            //surface runoff     
            double sourceSurfaceRunoff = 0; //(SR_TP_OS)
            if (result.P_NaturalAreas_WithVegetation > backgroundSurfaceRunoffTP)
                //sourceSurfaceRunoff = result.P_NaturalAreas_WithVegetation - backgroundSurfaceRunoffTP;
                sourceSurfaceRunoff = result.Emission_TP_SR - backgroundSurfaceRunoffTP;

            //Groundwater
            double sourceGroundwater = 0; //(GW_TP_SO)
            if (result.Emission_TP_GW > backgroundGroundwaterTP)
                sourceGroundwater = result.Emission_TP_GW - backgroundGroundwaterTP;

            //Zusammenführung der Ergebnisse und abschließende Berechnung der sources
            if (landUse.PavedArea == 0.0)
                result.P_SourceUrbanSettlements = result.Emission_TP_PointSources;
            else
                result.P_SourceUrbanSettlements = result.Emission_TP_PointSources + result.Emission_TP_US;

            if (landUse.AgriculturalLand == 0.0) //keine Landwirtschaftliche Fläche
            {
                result.P_SourceAgriculture = 0.0;
                result.P_SourceOther = result.Emission_TP_Total - result.Emission_TP_BG - result.P_SourceUrbanSettlements;
            }
            else
            {
                result.P_SourceAgriculture = result.TPErosionArableLand + result.TPErosionGrassland + result.TPSurfaceRunoffArableLand + result.TPSurfaceRunoffGrassland + result.TPGroundwaterArableLand + result.TPGroundwaterGrassland + result.Emission_TP_TD_ArableLand + result.Emission_TP_TD_Grassland - (result.Emission_TP_BG * landUse.AgriculturalLand / analyticalUnit.Area);
                result.P_SourceOther = result.Emission_TP_Total - result.Emission_TP_BG - result.P_SourceUrbanSettlements - result.P_SourceAgriculture;
            }

            if (result.P_SourceAgriculture < 0)
            {
                result.P_SourceAgriculture = 0.0;
                result.P_SourceOther = result.Emission_TP_Total - result.Emission_TP_BG - result.P_SourceUrbanSettlements;
            }

            if (result.P_SourceOther < 0.0)
            {
                result.P_SourceOther = 0.0;
                result.Emission_TP_BG = result.Emission_TP_Total - result.P_SourceUrbanSettlements - result.P_SourceAgriculture;
            }

            if (result.Emission_TP_BG < 0.0)
            {
                result.Emission_TP_BG = 0.0;
                result.P_SourceUrbanSettlements = result.Emission_TP_Total;
            }

            #endregion
        }
        /// <summary>
        /// Engine type
        /// </summary>
        public override EngineType EngineType
        {
            get { return EngineType.EmissionsTP; }
        }
        /// <summary>
        /// Version
        /// </summary>
        public override string Version
        {
            get { return Properties.Resources.VERSION; }
        }
        /// <summary>
        /// Description
        /// </summary>
        public override string Description
        {
            get { return Properties.Resources.STANDARD; }
        }
    }
}
