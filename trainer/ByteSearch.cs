﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace trainer
{
	/// <summary>
	/// Raised when the pattern given to a ByteSearch is invalid.
	/// </summary>
	public class InvalidBytePatternException : Exception {}
	
	/// <summary>
	/// Allows for searching of patterns in an array of bytes.
	/// </summary>
	public class ByteSearch
	{
		private struct BytePatternEntry
		{
			public byte Value;
			public byte Mask;
		}

		/// <summary>
		/// The size of the pattern.
		/// </summary>
		public int Size
		{
			get
			{
				return this.Values.Count;
			}
		}

		private List<BytePatternEntry> Values = new List<BytePatternEntry>();
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pattern"></param>
		public ByteSearch(string pattern)
		{
			Regex regex;
			
			regex = new Regex("^(([0-9,a-f]{2})|([x]{2})|(\\s))+$", RegexOptions.IgnoreCase);
			
			if (regex.Match(pattern).Success == false)
			{
				throw new InvalidBytePatternException();
			}

			regex = new Regex("(([0-9,a-f]{2})|([x]{2}))(?:\\s*)", RegexOptions.IgnoreCase);
			
			foreach (Match match in regex.Matches(pattern))
			{
				Debug.Assert(match.Captures.Count == 1);
				
				BytePatternEntry entry;
				string capture = match.Captures[0].Value.Trim().ToLower();

				if (capture == "xx")
				{
					entry.Value = 0xFF;
					entry.Mask = 0x00;
				}
				else
				{
					entry.Value = byte.Parse(capture, NumberStyles.HexNumber);
					entry.Mask = 0xFF;
				}

				this.Values.Add(entry);
			}
		}

		/// <summary>
		/// Match an array of bytes against the pattern.
		/// </summary>
		/// <param name="data"></param>
		/// <returns>True if it matches the pattern</returns>
		public UInt32 Match(byte[] data)
		{
			return this.Match(data, data.Length);
		}

		/// <summary>
		/// Match an array of bytes against the pattern.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="size">Size of the data</param>
		/// <returns>True if it matches the pattern</returns>
		public UInt32 Match(byte[] data, int size)
		{
			for (int i = 0; i < (size - (size % this.Size)); i++)
			{
				bool matched = true;
				for (int j = 0; matched && (j < this.Size); j++)
				{
					matched =
						(this.Values[j].Value & this.Values[j].Mask)
						==
						(data[i + j] & this.Values[j].Mask);
				}

				if (matched)
				{
					return (uint)i;
				}
			}

			return UInt32.MaxValue;
		}
	}
}
