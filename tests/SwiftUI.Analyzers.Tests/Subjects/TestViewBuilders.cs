using System;
using SwiftUI;

public static class M
{
	public static bool Cond = true;
	public static object Zero () => null;
	public static object One (object arg) => arg;
}

public class EmptyBlock
{
	public static void Build ()
	{
	}
}

public class ZeroBlock
{
	public static void Build ()
	{
		M.Zero ();
	}
}

public class ZeroZeroBlock
{
	public static void Build ()
	{
		M.Zero ();
		M.Zero ();
	}
}

public class OneZeroBlock
{
	public static void Build ()
	{
		M.One (M.Zero ());
	}
}

public class OptionalZeroBlock
{
	public static void Build ()
	{
		if (true)
			M.Zero ();
	}
}

public class OptionalZeroZeroBlock
{
	public static void Build ()
	{
		if (M.Cond) {
			M.Zero ();
			M.Zero ();
		}
	}
}

public class EitherZeroZeroBlock
{
	public static void Build ()
	{
		if (M.Cond)
			M.Zero ();
		else
			M.Zero ();
	}
}