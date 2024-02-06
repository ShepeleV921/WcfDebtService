using System;

namespace Tools.Classes
{
    public struct Fraction : IEquatable<Fraction>, IComparable<Fraction>
    {
        public int Numerator;

        public int Denominator;

        private bool _dontSimplify;
        public bool DontSimplify
        {
            get { return _dontSimplify; }
            set
            {
                _dontSimplify = value;
                Simplify();
            }
        }


        public Fraction(int numerator, bool dontSimplify = true)
        {
            Numerator = numerator;
            Denominator = 1;
            _dontSimplify = dontSimplify;
        }

        public Fraction(int numerator, int denominator, bool dontSimplify = true)
        {
            if (denominator == 0)
                throw new ArgumentException("Знаменатель дроби не может быть нулём");

            _dontSimplify = dontSimplify;

            if (dontSimplify)
            {
                Numerator = numerator;
                Denominator = denominator;
            }
            else
            {
                int gcd = VdkFuncs.GreatestCommonDivisor(numerator, denominator);

                if (numerator >= 0 && denominator < 0 ||
                    numerator <= 0 && denominator < 0)
                {
                    Numerator = -numerator / gcd;
                    Denominator = -denominator / gcd;
                }
                else
                {
                    Numerator = numerator / gcd;
                    Denominator = denominator / gcd;
                }
            }
        }

        public void Simplify()
        {
            if (DontSimplify)
                return;

            int gcd = VdkFuncs.GreatestCommonDivisor(Numerator, Denominator);

            Numerator /= gcd;
            Denominator /= gcd;
        }

        public int CompareTo(Fraction other)
        {
            if (Denominator != other.Denominator)
            {
                long ay = Numerator * other.Denominator;
                long bx = Denominator * other.Numerator;

                if (ay == bx)
                    return 0;

                return ay > bx ? 1 : -1;
            }
            else
            {
                if (Numerator == other.Numerator)
                    return 0;

                return Numerator > other.Numerator ? 1 : -1;
            }
        }

        public bool Equals(Fraction other)
        {
            if (other.Numerator == 0 && Numerator == 0)
                return true;

            if (other.Numerator == 0 || Numerator == 0)
                return false;

            return ((long)Numerator * other.Denominator) / ((long)other.Numerator * Denominator) == 1L;
        }

        public override string ToString()
        {
            if (Denominator == 1)
                return Numerator.ToString();

            return Numerator.ToString() + "/" + Denominator.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is Fraction)
                return Equals((Fraction)obj);

            return false;
        }

        public override int GetHashCode()
        {
            return Numerator ^ ((Denominator << 8) | (Denominator >> 8));
        }


        public static bool operator ==(Fraction x, Fraction y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Fraction x, Fraction y)
        {
            return !x.Equals(y);
        }

        public static bool operator >(Fraction x, Fraction y)
        {
            return x.CompareTo(y) == 1;
        }

        public static bool operator <(Fraction x, Fraction y)
        {
            return x.CompareTo(y) == -1;
        }

        public static bool operator >=(Fraction x, Fraction y)
        {
            return x.CompareTo(y) >= 0;
        }

        public static bool operator <=(Fraction x, Fraction y)
        {
            return x.CompareTo(y) <= 0;
        }


        public static Fraction operator +(Fraction x, Fraction y)
        {
            if (x.Denominator != y.Denominator)
            {
                long num = x.Numerator * y.Denominator + x.Denominator * y.Numerator;
                long den = x.Denominator * y.Denominator;

                long gcd = VdkFuncs.GreatestCommonDivisor(num, den);

                return new Fraction((int)(num / gcd), (int)(den / gcd), x.DontSimplify || y.DontSimplify);
            }
            else
            {
                return new Fraction(x.Numerator + y.Numerator, x.Denominator, x.DontSimplify || y.DontSimplify);
            }
        }

        public static Fraction operator -(Fraction x, Fraction y)
        {
            if (x.Denominator != y.Denominator)
            {
                long num = x.Numerator * y.Denominator - x.Denominator * y.Numerator;
                long den = x.Denominator * y.Denominator;

                long gcd = VdkFuncs.GreatestCommonDivisor(num, den);

                return new Fraction((int)(num / gcd), (int)(den / gcd), x.DontSimplify || y.DontSimplify);
            }
            else
            {
                return new Fraction(x.Numerator + y.Numerator, x.Denominator, x.DontSimplify || y.DontSimplify);
            }
        }

        public static Fraction operator -(Fraction x)
        {
            return new Fraction(-x.Numerator, x.Denominator, x.DontSimplify);
        }

        public static Fraction operator *(Fraction x, Fraction y)
        {
            long num = x.Numerator * y.Numerator;
            long den = x.Denominator * y.Denominator;

            long gcd = VdkFuncs.GreatestCommonDivisor(num, den);

            return new Fraction((int)(num / gcd), (int)(den / gcd), x.DontSimplify || y.DontSimplify);
        }

        public static Fraction operator /(Fraction x, Fraction y)
        {
            long num = x.Numerator * y.Denominator;
            long den = x.Denominator * y.Numerator;

            long gcd = VdkFuncs.GreatestCommonDivisor(num, den);

            return new Fraction((int)(num / gcd), (int)(den / gcd), x.DontSimplify || y.DontSimplify);
        }


        public static Fraction operator +(Fraction x, int y)
        {
            return new Fraction(x.Numerator + y * x.Denominator, x.Denominator, x.DontSimplify);
        }

        public static Fraction operator -(Fraction x, int y)
        {
            return new Fraction(x.Numerator - y * x.Denominator, x.Denominator, x.DontSimplify);
        }

        public static Fraction operator *(Fraction x, int y)
        {
            return new Fraction(x.Numerator * y, x.Denominator, x.DontSimplify);
        }

        public static Fraction operator /(Fraction x, int y)
        {
            return new Fraction(x.Numerator, x.Denominator * y, x.DontSimplify);
        }


        public static Fraction operator +(int x, Fraction y)
        {
            return y + x;
        }

        public static Fraction operator -(int x, Fraction y)
        {
            return y - x;
        }

        public static Fraction operator *(int x, Fraction y)
        {
            return y * x;
        }

        public static Fraction operator /(int x, Fraction y)
        {
            return new Fraction(x * y.Denominator, y.Numerator, y.DontSimplify);
        }


        public static double operator +(Fraction x, double y)
        {
            return (1.0 * x.Numerator / x.Denominator) + y;
        }

        public static double operator -(Fraction x, double y)
        {
            return (1.0 * x.Numerator / x.Denominator) - y;
        }

        public static double operator *(Fraction x, double y)
        {
            return y * x.Numerator / x.Denominator;
        }

        public static double operator /(Fraction x, double y)
        {
            return (1.0 * x.Numerator / x.Denominator) / y;
        }


        public static double operator +(double x, Fraction y)
        {
            return y + x;
        }

        public static double operator -(double x, Fraction y)
        {
            return y - x;
        }

        public static double operator *(double x, Fraction y)
        {
            return y * x;
        }

        public static double operator /(double x, Fraction y)
        {
            return x * y.Denominator / y.Numerator;
        }


        public static decimal operator +(Fraction x, decimal y)
        {
            return (1.0M * x.Numerator / x.Denominator) + y;
        }

        public static decimal operator -(Fraction x, decimal y)
        {
            return (1.0M * x.Numerator / x.Denominator) - y;
        }

        public static decimal operator *(Fraction x, decimal y)
        {
            return y * x.Numerator / x.Denominator;
        }

        public static decimal operator /(Fraction x, decimal y)
        {
            return (1.0M * x.Numerator / x.Denominator) / y;
        }


        public static decimal operator +(decimal x, Fraction y)
        {
            return y + x;
        }

        public static decimal operator -(decimal x, Fraction y)
        {
            return y - x;
        }

        public static decimal operator *(decimal x, Fraction y)
        {
            return y * x;
        }

        public static decimal operator /(decimal x, Fraction y)
        {
            return x * y.Denominator / y.Numerator;
        }


        public static implicit operator Fraction(int other)
        {
            return new Fraction(other);
        }
    }
}
