// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// For some reason, my MDK version likes prohibiting things that aren't actually prohibited, not sure if it's the MDK or VS
#pragma warning disable ProhibitedMemberRule // Prohibited Type Or Member
[assembly: SuppressMessage("Whitelist", "ProhibitedMemberRule:Prohibited Type Or Member", Justification = "<Pending>", Scope = "member", Target = "~M:IngameScript.Program.LegGroup.SetAnglesOf(System.Collections.Generic.List{IngameScript.Program.Joint},System.Collections.Generic.List{IngameScript.Program.Joint},System.Double,System.Double,System.Double)")]
#pragma warning restore ProhibitedMemberRule // Prohibited Type Or Member
