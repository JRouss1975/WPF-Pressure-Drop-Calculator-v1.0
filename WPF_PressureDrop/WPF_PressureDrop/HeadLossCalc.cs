using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using MathNet.Numerics;
using BasicInterpolation;
using System.IO.Packaging;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace WPF_PressureDrop
{
    //[TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ItemType
    {
        //[Description("Pipe")]
        Pipe,
        Reducer,
        Expander,
        Bend,
        Mitre,
        Tee,
        Butterfly,
        Check,
        Stop,
        Ball,
        Gate,
        Swing,
        Globe,
        Lift,
        Entrance,
        Exit,
        Component
    }

    [Serializable]
    [DataContract]
    public class HeadLossCalc : Observable, IComparable<HeadLossCalc>, ICloneable
    {

        #region CONSTRUCTORS
        public HeadLossCalc()
        {

        }

        public HeadLossCalc(int id)
        {
            this.ElementId = id;
        }
        #endregion

        #region DATA-TABLES
        /// <summary>
        /// gravitational acceleration [m/sec2].
        /// </summary>
        const double g = 9.80665;

        //range of temperatures for density calculation[F]
        private double[] temps = new double[] { 0.0, 10.0, 20.0, 25, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0, 110.0, 120.0 };

        //sea water densities for salinity 35 [kg/m3] at pressure 1atm.
        private double[] waterDensities = new double[] { 1028.0, 1027.0, 1024.9, 1023.6, 1022.0, 1018.3, 1014.0, 1009.0, 1003.5, 997.5, 991.0, 984.2, 976.2, 969.3 };

        //sea water viscosities for salinity 35 [cP] at pressure 1atm.
        private double[] waterViscosities = new double[] { 1.906, 1.397, 1.077, 0.959, 0.861, 0.707, 0.594, 0.508, 0.441, 0.393, 0.349, 0.313, 0.280, 0.258 };

        //Bend ratios
        private double[] bendRatio = new double[] { 1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.0, 10.0, 12.0, 14.0, 16.0, 20.0 };

        //Bend K values
        private double[] bendK = new double[] { 20.0, 14.0, 12.0, 12.0, 14.0, 17.0, 24.0, 30.0, 34.0, 38.0, 42.0, 50.0 };

        //Mitre Bend angles
        private double[] bendAngle = new double[] { 0.0, 15.0, 30.0, 45.0, 60.0, 75.0, 90.0 };

        //Bend K values
        private double[] mitreBendK = new double[] { 2.0, 4.0, 8.0, 15.0, 25.0, 40.0, 60.0 };
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Notes
        /// </summary>
        public string Notes { get; set; } = "";


        /// <summary>
        /// Element Id
        /// </summary>
        private int _elementId;
        public int ElementId
        {
            get { return _elementId; }
            set
            {
                if (value != _elementId)
                {
                    _elementId = value;
                    NotifyChange("");
                }
            }
        }
        /// <summary>
        /// Start Node.
        /// </summary>
        public Node Node1 { get; set; } = new Node();

        /// <summary>
        /// End Node.
        /// </summary>
        public Node Node2 { get; set; } = new Node();

        /// <summary>
        /// Icon 
        /// </summary>
        public string Icon
        {
            get
            {
                if (this.ElementType == ItemType.Pipe) return "/Resources/Pipe.png";
                if (this.ElementType == ItemType.Bend) return "/Resources/Bend.png";
                if (this.ElementType == ItemType.Expander) return "/Resources/Expander.png";
                if (this.ElementType == ItemType.Reducer) return "/Resources/Reducer.png";
                if (this.ElementType == ItemType.Butterfly || this.ElementType == ItemType.Check) return "/Resources/ButterflyValve.png";
                return "/Resources/MainIcon.png";
            }
        }

        /// <summary>
        /// Temperature degC.
        /// </summary>
        private double _t;
        public double t
        {
            get { return _t; }
            set
            {
                if (value != _t)
                {
                    _t = value;
                    NotifyChange("");
                }
            }
        }

        /// <summary>
        /// ε, absolute roughness or effective height of pipe wall irregularities (mm).
        /// </summary>
        /// 
        private double _epsilon;
        public double epsilon
        {
            get { return _epsilon; }
            set
            {
                if (value != _epsilon)
                {
                    _epsilon = value;
                    NotifyChange("");
                }
            }
        }

        /// <summary>
        /// qh, rate of flow at flowing conditions m3/h,
        /// </summary>
        private double _qh;
        public double qh
        {
            get { return _qh; }
            set
            {
                if (value != _qh)
                {
                    _qh = value;
                    NotifyChange("");
                }
            }
        }

        /// <summary>
        /// Type of fitting
        /// </summary>
        public ItemType ElementType { get; set; }

        /// <summary>
        /// Length of pipe (m).
        /// </summary>
        private double _L;
        public double L
        {
            get { return _L; }
            set
            {
                if (value != _L)
                {
                    _L = value;
                    NotifyChange("");
                }
            }
        }

        /// <summary>
        /// Internal diameter (m).
        /// </summary>
        private double _d;
        public double d
        {
            get { return _d; }
            set
            {
                if (value != _d)
                {
                    _d = value;
                    NotifyChange("");
                }
            }
        }

        /// <summary>
        /// Reducer/Expander small diamerer d1 in [mm].
        /// </summary>
        public double d1 { get; set; }

        /// <summary>
        /// Reducer/Expander large diamerer d2 in [mm].
        /// </summary>
        public double d2 { get; set; }

        /// <summary>
        /// Bend radious r in [mm].
        /// </summary>
        public double r { get; set; }

        /// <summary>
        /// Number of bends n.
        /// </summary>
        public double n { get; set; }

        /// <summary>
        /// Mitre Bend angle a in [deg].
        /// </summary>
        public double a { get; set; }

        /// <summary>
        /// Weight of item W in [kg].
        /// </summary>
        public double W { get; set; }
        #endregion

        #region CALCULATED PROPERTIES
        /// <summary>
        /// ρ, water density kg/m3.
        /// </summary>
        public double rho
        {
            get
            {
                double[] p = Fit.Polynomial(temps, waterDensities, 13);
                return Polynomial.Evaluate(t, p);
            }
        }

        /// <summary>
        /// μ, Dynamic viscocity in cP.
        /// </summary>
        public double me
        {
            get
            {
                double[] p = Fit.Polynomial(temps, waterViscosities, 13);
                return Polynomial.Evaluate(t, p);
            }
        }

        /// <summary>
        /// ν, Kinematic viscocity in cSt.
        /// </summary>
        public double ne
        {
            get { return me / (rho / 1000); }
        }

        /// <summary>
        /// Internal diameter (mm).
        /// </summary>
        private double D
        {
            get
            {
                return d / 1000;
            }
        }

        /// <summary>
        /// Reducer/Expander small diamerer D1 in [m].
        /// </summary>
        private double D1
        {
            get { return d1 / 1000; }
        }

        /// <summary>
        /// Reducer/Expander large diamerer D2 in [m].
        /// </summary>
        private double D2
        {
            get { return d2 / 1000; }
        }

        /// <summary>
        /// q, rate of flow at flowing conditions m3/sec,
        /// </summary>
        private double q
        {
            get { return qh / 3600; }
        }

        /// <summary>
        /// Q, rate of flow at flowing conditions lts/min.
        /// </summary>
        public double Q
        {
            get { return q * 60000; }
        }

        /// <summary>
        /// A, pipe sectional area m2,
        /// </summary>
        public double A
        {
            get
            {
                return (Math.PI / 4) * (D * D);
            }
        }

        /// <summary>
        /// Volume of item V in [liters].
        /// </summary>
        public double Vm
        {
            get
            {
                if (ElementType == ItemType.Reducer || ElementType == ItemType.Expander)
                {
                    double A1 = (Math.PI / 4) * Math.Pow(D1, 2);
                    double A2 = (Math.PI / 4) * Math.Pow(D2, 2);
                    return ((A1 + A2) / 2) * L * 1000;
                }
                return A * L * 1000;
            }
        }

        /// <summary>
        /// v, mean flow velocity m/sec.
        /// </summary>
        public double v
        {
            get { return A > 0 ? q / A : -1; }
        }

        /// <summary>
        /// Reynolds number (unitless).
        /// </summary>
        public double Re
        {
            get { return (d * v * rho) / me; }
        }

        /// <summary>
        /// Completley turbulent friction factor (Equation 2-8).
        /// </summary>
        public double fT
        {
            get
            {
                return 0.25 / Math.Pow(Math.Log10(epsilon / (3.7 * d)), 2);
            }
        }

        /// <summary>
        /// Serghide’s Solution
        /// </summary>
        private double fs
        {
            get
            {
                double rel_ε = epsilon / d;
                double A = -2 * Math.Log10((rel_ε / 3.7) + (12 / Re));
                double B = -2 * Math.Log10((rel_ε / 3.7) + (2.51 * A / Re));
                double C = -2 * Math.Log10((rel_ε / 3.7) + (2.51 * B / Re));
                return Math.Pow((A - (Math.Pow((B - A), 2) / (C - 2 * B + A))), -2);
            }
        }

        /// <summary>
        /// Colebrook equation friction factor.
        /// </summary>
        public double f
        {
            get
            {
                if (Re < 2000)
                    return 64 / Re;

                if (Re > 4000)
                {
                    double rel_ε = epsilon / d;
                    double f1 = .0000000001;
                    double f2 = 0;
                    do
                    {
                        f2 = f1;
                        f1 = 1 / Math.Pow(-2 * Math.Log10(rel_ε / 3.7 + 2.51 / (Re * Math.Sqrt(f2))), 2);
                    }
                    while (Math.Abs(f1 - f2) > .0000000001);
                    return f1;
                }

                return -1;
            }
        }

        /// <summary>
        /// Loss of static pressure head due to fluid flow [m].
        /// </summary>
        public double hL
        {
            get
            {
                return K * ((v * v) / (2 * g));
            }
        }

        /// <summary>
        /// Loss of static pressure head due to fluid flow [mm].
        /// </summary>
        public double hLm
        {
            get { return hL * 1000; }
        }

        /// <summary>
        /// Resistance coefficient.
        /// </summary>
        public double K
        {
            get
            {
                double k = 0, k1, k2, kb;
                double theta = Math.Abs(2 * Math.Atan((d2 - d1) / (2 * (L * 1000))) * (180 / Math.PI));
                double beta = d1 / d2;
                double rd;
                double r = this.r;

                switch (ElementType)
                {
                    case ItemType.Pipe:
                        k = f * (L / D);
                        break;

                    case ItemType.Reducer:
                        if (theta <= 45)
                        {
                            k1 = (0.8 * Math.Sin((theta / 2) * (Math.PI / 180)) * (1 - Math.Pow(beta, 2)));
                            k2 = k1 / Math.Pow(beta, 4);
                            k = k2;
                        }
                        if (45 < theta && theta <= 180)
                        {
                            k1 = (0.5 * Math.Sqrt(Math.Sin((theta / 2) * (Math.PI / 180))) * (1 - Math.Pow(beta, 2)));
                            k2 = k1 / Math.Pow(beta, 4);
                            k = k2;
                        }
                        break;

                    case ItemType.Expander:
                        if (theta <= 45)
                        {
                            k1 = (2.6 * Math.Sin((theta / 2) * (Math.PI / 180)) * Math.Pow((1 - Math.Pow(beta, 2)), 2));
                            k2 = k1 / Math.Pow(beta, 4);
                            k = k1;
                        }
                        if (45 < theta && theta <= 180)
                        {
                            k1 = Math.Pow((1 - Math.Pow(beta, 2)), 2);
                            k2 = k1 / Math.Pow(beta, 4);
                            k = k1;
                        }
                        break;

                    case ItemType.Bend:
                        if (r / d > 1.25 && r / d < 1.75)
                            rd = 1.5;
                        else
                            rd = Math.Round((r / d), MidpointRounding.ToEven);

                        if (rd >= 1 && rd <= 20)
                        {
                            LinearInterpolation LI0 = new LinearInterpolation(bendRatio, bendK);
                            kb = (double)LI0.Interpolate(rd);
                            if (n == 0)
                                k = kb * fT;
                            else
                                k = ((n - 1) * ((0.25 * Math.PI * fT * (r / d)) + (0.5 * kb)) + kb) * fT;
                        }
                        else
                        {
                            k = -1;
                        }
                        break;

                    case ItemType.Tee:
                        break;

                    case ItemType.Butterfly:
                        if (d >= 0 && d < 250) k = 45 * fT;
                        if (d >= 250 && d < 400) k = 35 * fT;
                        if (d >= 400) k = 25 * fT;
                        break;

                    case ItemType.Check:
                        if (d >= 0 && d < 250) k = 120 * fT;
                        if (d >= 250 && d < 400) k = 90 * fT;
                        if (d >= 400) k = 60 * fT;
                        break;

                    case ItemType.Lift:
                        k = 600 * fT;
                        break;

                    case ItemType.Globe:
                        k = 600 * fT;
                        break;

                    case ItemType.Gate:
                        k = 8 * fT;
                        break;

                    case ItemType.Swing:
                        k = 100 * fT;
                        break;

                    case ItemType.Ball:
                        k = 3 * fT;
                        break;

                    case ItemType.Entrance:
                        k = 0.78;
                        break;

                    case ItemType.Exit:
                        k = 1;
                        break;

                    case ItemType.Stop:
                        k = 400 * fT;
                        break;

                    case ItemType.Mitre:
                        LinearInterpolation LI1 = new LinearInterpolation(bendAngle, mitreBendK);
                        k = (double)LI1.Interpolate(a) * fT;
                        break;

                    case ItemType.Component:
                        break;

                    default:
                        break;
                }

                return k;
            }
        }
        #endregion

        #region METHODS
        public double Distance(double x1, double x2, double y1, double y2, double z1, double z2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2));
        }

        public int CompareTo(HeadLossCalc other)
        {
            //if (this.Node1.X == other.Node1.X && this.Node1.Y == other.Node1.Y && this.Node1.Z == other.Node1.Z && this.Node2.X == other.Node2.X && this.Node2.Y == other.Node2.Y && this.Node2.Z == other.Node2.Z) return 0;
            //if (this.Node1.X < other.Node1.X && this.Node1.Y < other.Node1.Y && this.Node1.Z < other.Node1.Z && this.Node2.X < other.Node2.X && this.Node2.Y < other.Node2.Y && this.Node2.Z < other.Node2.Z) return -1;
            //return 1;
            return this.ElementId.CompareTo(other.ElementId);
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }

    public class Node
    {
        public int NodeId;
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

    }

    public class ItemEqualityComparer : IEqualityComparer<HeadLossCalc>
    {
        public bool Equals(HeadLossCalc x, HeadLossCalc y)
        {
            return x.Node1.X == y.Node1.X && x.Node1.Y == y.Node1.Y && x.Node1.Z == y.Node1.Z &&
                   x.Node2.X == y.Node2.X && x.Node2.Y == y.Node2.Y && x.Node2.Z == y.Node2.Z &&
                   x.L == y.L &&
                   x.d == y.d &&
                   x.ElementType == y.ElementType;
        }

        public int GetHashCode(HeadLossCalc obj)
        {
            return -1;
        }
    }
}