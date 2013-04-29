using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class Asciifyer
	{
		public static Dictionary<string, string> Mapping = new Dictionary<string, string>
			{
				{"Α", "ALPHA"},
				{"α", "alpha"},
				{"Β", "BETA"},
				{"β", "beta"},
				{"Γ", "GAMMA"},
				{"γ", "gamma"},
				{"Δ", "DELTA"},
				{"δ", "delta"},
				{"Ε", "EPSILON"},
				{"ε", "epsilon"},
				{"Ζ", "ZETA"},
				{"ζ", "zeta"},
				{"Η", "ETA"},
				{"η", "eta"},
				{"Θ", "theta"},
				{"θ", "THETA"},
				{"Ι", "IOTA"},
				{"ι", "iota"},
				{"Κ", "KAPPA"},
				{"κ", "kappa"},
				{"Λ", "LAMBDA"},
				{"λ", "lambda"},
				{"Μ", "MU"},
				{"μ", "mu"},
				{"Ν", "NU"},
				{"ν", "nu"},
				{"Ξ", "XI"},
				{"ξ", "xi"},
				{"Ο", "OMICRON"},
				{"ο", "omicron"},
				{"Π", "PI"},
				{"π", "pi"},
				{"Ρ", "RHO"},
				{"ρ", "rho"},
				{"Σ", "SIGMA"},
				{"σ", "sigma"},
				{"Τ", "TAU"},
				{"τ", "tau"},
				{"Υ", "UPSILON"},
				{"υ", "upsilon"},
				{"Φ", "PHI"},
				{"φ", "phi"},
				{"Χ", "CHI"},
				{"χ", "chi"},
				{"Ψ", "PSI"},
				{"ψ", "psi"},
				{"Ω", "OMEGA"},
				{"ω", "omega"}
		};

		public static string Translate(string s)
		{
			foreach (var pair in Mapping)
			{
				s = s.Replace(pair.Key, pair.Value);
			}

			return s;
		}
	}
}

