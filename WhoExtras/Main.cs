using System;
using System.Collections.Generic;
using System.Linq;
using Crimson.CustomEvents.Extensions;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace WhoExtras
{
	[ApiVersion(2, 0)]
	public class WhoExtras : TerrariaPlugin
	{
		public WhoExtras(Main game) : base(game)
		{
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
		}

		private static void OnInitialize(EventArgs args)
		{
			// From EssentialsPlus
			Action<Command> Add = c =>
			{
				//Finds any commands with names and aliases that match the new command and removes them.
				Commands.ChatCommands.RemoveAll(c2 => c2.Names.Exists(s2 => c.Names.Contains(s2)));
				//Then adds the new command.
				Commands.ChatCommands.Add(c);
			};

			Add(new Command(ListConnectedPlayers, "playing", "online", "who")
			{
				HelpText = "Shows the currently connected players."
			});
		}

		private static IEnumerable<string> GetPlayers(bool pvp, bool displayid, bool realsender)
		{
			var players = pvp
				? TShock.Players.Where(p => p != null && p.Active && p.TPlayer.hostile)
				: TShock.Players.Where(p => p != null && p.Active);

			Func<TSPlayer, string> namer;

			if (realsender)
				namer = p => p.Name.Colorize(new Color(p.Group.R, p.Group.G, p.Group.B));
			else
				namer = p => p.Name;

			return displayid
				? players.Select(p => string.Format("{0} (IX: {1}){2}",
					namer(p),
					p.Index,
					p.User != null ? $" (ID: {p.User.ID})" : string.Empty
				))
				: players.Select(p => namer(p));
		}

		private static void ListConnectedPlayers(CommandArgs args)
		{
			bool invalidUsage = args.Parameters.Count > 3;

			var displayIdsRequested = false;
			var displaypvp = false;
			var pageNumber = 1;

			if (!invalidUsage)
				foreach (string parameter in args.Parameters)
				{
					if (parameter.Equals("-i", StringComparison.InvariantCultureIgnoreCase))
					{
						displayIdsRequested = true;
						continue;
					}

					if (parameter.Equals("-p", StringComparison.InvariantCultureIgnoreCase))
					{
						displaypvp = true;
						continue;
					}

					if (!int.TryParse(parameter, out pageNumber))
					{
						invalidUsage = true;
						break;
					}
				}
			if (invalidUsage)
			{
				args.Player.SendErrorMessage("Invalid usage, proper usage: {0}who [-p] [-i] [pagenumber]", Commands.Specifier);
				return;
			}
			if (displayIdsRequested && !args.Player.HasPermission(Permissions.seeids))
			{
				args.Player.SendErrorMessage("You don't have the required permission to list player ids.");
				return;
			}

			Func<string> getParam = () =>
			{
				if (displayIdsRequested && displaypvp)
				{
					return " -i -p";
				}

				if (displaypvp)
				{
					return " -p";
				}

				return displayIdsRequested ? " -i" : string.Empty;
			};

			var players = GetPlayers(displaypvp, displayIdsRequested, args.Player.RealPlayer).ToList();

			args.Player.SendSuccessMessage("Online Players{2}: ({0}/{1})",
				players.Count,
				TShock.Config.MaxSlots,
				displaypvp ? " (with PvP enabled)" : string.Empty);

			PaginationTools.SendPage(
				args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(players, maxCharsPerLine: 140),
				new PaginationTools.Settings
				{
					IncludeHeader = false,
					LineTextColor = Color.White,
					FooterTextColor = Color.Green,
					FooterFormat =
						string.Format("Type {0}who{1} {{0}} for more.", Commands.Specifier, getParam())
				}
			);
		}

		protected override void Dispose(bool disposing)
		{
			ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
			base.Dispose(disposing);
		}

		#region Meta

		public override string Name => "WhoExtras";
		public override string Author => "Newy";
		public override string Description => "Extends the /who command with extra features.";
		public override Version Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

		#endregion
	}
}