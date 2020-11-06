﻿using ExampleMod.Content.Tiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Container;
using Terraria.ModLoader.IO;

namespace ExampleMod.Content.TileEntities
{
	public class ItemCollectorTE : ModTileEntity, IItemHandler
	{
		private const int Range = 10 * 16;
		private const int Speed = 30;

		private int timer;
		private ItemHandler ItemHandler;

		public ItemCollectorTE() {
			ItemHandler = new ItemHandler(18);
		}

		public override void Update() {
			if (++timer >= Speed) {
				timer = 0;
				Vector2 center = new Vector2((Position.X * 16) + 16, (Position.Y * 16) + 16);

				for (int i = 0; i < Main.item.Length; i++) {
					ref Item item = ref Main.item[i];
					if (!item.active || item.IsAir) continue;
					if (Vector2.DistanceSquared(item.Center, center) > Range * Range) continue;

					ItemHandler.InsertItem(ref item);
				}
			}
		}

		public override bool ValidTile(int i, int j) {
			Tile tile = Main.tile[i, j];
			return tile.active() && tile.type == ModContent.TileType<ItemCollector>() && tile.frameX == 0 && tile.frameY == 0;
		}

		public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetMessage.SendTileSquare(Main.myPlayer, i - 1, j - 1, 3);
				NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j - 1, Type);
				return -1;
			}

			return Place(i, j - 1);
		}

		public override void OnKill() {
			ItemHandler.DropItems(new Rectangle(Position.X * 16, Position.Y * 16, 32, 32));
		}

		public override TagCompound Save() {
			return ItemHandler.Save();
		}

		public override void Load(TagCompound tag) {
			ItemHandler.Load(tag);
		}

		public ItemHandler GetItemHandler() => ItemHandler;
	}
}