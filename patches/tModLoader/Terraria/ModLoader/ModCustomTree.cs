﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Default;
using Terraria.ObjectData;

namespace Terraria.ModLoader;

public abstract class ModCustomTree : ModType, ILocalizedModType
{
	/// <summary>
	/// Texture path for tree tile
	/// </summary>
	public virtual string TileTexture => (GetType().Namespace + "." + Name + "_Tile").Replace('.', '/');

	/// <summary>
	/// Texture path for tree sapling tile.<br/>
	/// Can contain multiple styles of saplings, style count is set by <see cref="SaplingStyles"/>
	/// </summary>
	public virtual string SaplingTexture => (GetType().Namespace + "." + Name + "_Sapling").Replace('.', '/');

	/// <summary>
	/// Texture path for tree acorn item
	/// </summary>
	public virtual string AcornTexture => (GetType().Namespace + "." + Name + "_Acorn").Replace('.', '/');

	/// <summary>
	/// Texture path for tree top<br/>
	/// Must contain 3 variants of tops<br/>
	/// Sprite size is defined in <see cref="GetTreeFoliageData"/>
	/// </summary>
	public virtual string TopTexture => (GetType().Namespace + "." + Name + "_Top").Replace('.', '/');

	/// <summary>
	/// Texture path for tree top<br/>
	/// Must contain 3 variants of branches on both sides<br/>
	/// </summary>
	public virtual string BranchTexture => (GetType().Namespace + "." + Name + "_Branch").Replace('.', '/');

	/// <summary>
	/// If this is provided, new leaf ModGore will be registered and used as LeafType
	/// </summary>
	public virtual string LeafTexture => null;

	/// <summary>
	/// Custom tree tile. Override if custom behavior needed. Defaults to <see cref="CustomTreeDefaultTile"/>.
	/// </summary>
	public virtual CustomTreeTile Tile { get; set; }

	/// <summary>
	/// Custom tree sapling. Override if custom behavior needed. Defaults to <see cref="CustomTreeDefaultSapling"/>.
	/// </summary>
	public virtual CustomTreeSapling Sapling { get; set; }

	/// <summary>
	/// Custom tree acorn. Override if custom behavior needed. Defaults to <see cref="CustomTreeDefaultAcorn"/>.
	/// </summary>
	public virtual CustomTreeAcorn Acorn { get; set; }

	/// <summary>
	/// Custom tree leaf. Override if custom behavior needed. Defaults to <see cref="CustomTreeDefaultLeaf"/> if <see cref="LeafTexture"/> is set.
	/// </summary>
	public virtual CustomTreeLeaf Leaf { get; set; }

	/// <summary>
	/// Tree style type, unique for each tree
	/// </summary>
	public int TreeStyle { get; internal set; }

	/// <summary>
	/// How many styles sapling tile texture has
	/// </summary>
	public virtual int SaplingStyles { get; set; } = 1;

	/// <summary>
	/// Tree shader settings for painting
	/// </summary>
	public TreePaintingSettings PaintingSettings = new TreePaintingSettings {
		UseSpecialGroups = true,
		SpecialGroupMinimalHueValue = 1f / 6f,
		SpecialGroupMaximumHueValue = 5f / 6f,
		SpecialGroupMinimumSaturationValue = 0f,
		SpecialGroupMaximumSaturationValue = 1f
	};

	/// <summary>
	/// Tree type for vanilla tree shake system
	/// </summary>
	public virtual TreeTypes TreeType { get; set; } = TreeTypes.Forest;

	/// <summary>
	/// Used for sapling and tree ground tile type checks
	/// </summary>
	public virtual int[] ValidGroundTiles { get; } = new int[] { 2 };

	/// <summary>
	/// Used for sapling wall type checks
	/// </summary>
	public virtual int[] ValidWalls { get; } = new int[] { 0 };

	/// <summary>
	/// 1 in X chance of tree growing from sapling per random tick
	/// </summary>
	public virtual int GrowChance { get; set; } = 5;

	/// <summary>
	/// Generation weight among other custom trees
	/// </summary>
	public virtual int GenerationWeight { get; set; } = 10;

	/// <summary>
	/// Chance that this tree will try to generate if chosen by weight
	/// </summary>
	public virtual int GenerationChance { get; set; } = 5;

	/// <summary>
	/// 1 in X chance of not generating roots
	/// </summary>
	public virtual int NoRootChance { get; set; } = 3;

	/// <summary>
	/// 1 in X chance of generating more bark on tile
	/// </summary>
	public virtual int MoreBarkChance { get; set; } = 7;

	/// <summary>
	/// 1 in X chance of generating less bark on tile
	/// </summary>
	public virtual int LessBarkChance { get; set; } = 7;

	/// <summary>
	/// 1 in X chance of generating branch
	/// </summary>
	public virtual int BranchChance { get; set; } = 4;

	/// <summary>
	/// 1 in X chance that generated branch will not have leaves
	/// </summary>
	public virtual int NotLeafyBranchChance { get; set; } = 3;

	/// <summary>
	/// 1 in X chance that generated top will be broken
	/// </summary>
	public virtual int BrokenTopChance { get; set; } = 13;

	/// <summary>
	/// Minimum tree height for growing from sapling (not growing over time)
	/// </summary>
	public virtual int MinHeight { get; set; } = 5;

	/// <summary>
	/// Maximum tree height for growing from sapling (not growing over time)
	/// </summary>
	public virtual int MaxHeight { get; set; } = 12;

	/// <summary>
	/// How many tiles after tree top tile are additionally checked if empty
	/// </summary>
	public virtual int TopPadding { get; set; } = 4;

	public string LocalizationCategory => "CustomTrees";

	private List<Asset<Texture2D>> FoliageTextureCache = new();

	/// <summary>
	/// Called when tree tries to grow from sapling<br/>
	/// Called from tree's sapling ModTile on <see cref="ModBlockType.RandomUpdate"/>. You'll need to call it manually when overriding tree tile with <see cref="LoadSapling"/>.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	public virtual bool Grow(int x, int y)
	{
		if (CustomTreeGenerator.GrowTree(x, y, GetTreeSettings()) && WorldGen.PlayerLOS(x, y))
		{
			WorldGen.TreeGrowFXCheck(x, y);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Gets current tree settings
	/// </summary>
	public CustomTreeGenerationSettings GetTreeSettings()
	{
		return new() {
			MaxHeight = MaxHeight,
			MinHeight = MinHeight,

			TreeTileType = Tile.Type,

			GroundTypeCheck = ValidGroundType,
			WallTypeCheck = ValidWallType,

			TopPaddingNeeded = TopPadding,

			BranchChance = BranchChance,
			BrokenTopChance = BrokenTopChance,
			LessBarkChance = LessBarkChance,
			MoreBarkChance = MoreBarkChance,
			NotLeafyBranchChance = NotLeafyBranchChance,
			NoRootChance = NoRootChance,
		};
	}

	/// <summary>
	/// Return true if tile type is valid for tree to grow on
	/// </summary>
	public virtual bool ValidGroundType(int tile) => ValidGroundTiles.Contains(tile);

	/// <summary>
	/// Return true if wall type is valid for tree
	/// </summary>
	public virtual bool ValidWallType(int tile) => ValidWalls.Contains(tile);

	/// <summary>
	/// This is executed when tree is being shook, return true to continue vanilla code for tree shaking
	/// </summary>
	public virtual bool Shake(int x, int y, ref bool createLeaves) => true;

	/// <summary>
	/// Gets tree leaf gore id and tree height for falling leaves
	/// </summary>
	public virtual void GetTreeLeaf(int x, Tile topTile, Tile t, ref int treeHeight, ref int treeFrame, out int passStyle)
	{
		passStyle = Leaf?.Type ?? GoreID.TreeLeaf_Normal;
	}

	/// <summary>
	/// Called in world generaion process
	/// Return true if tree was generated
	/// </summary>
	/// <param name="x">Ground tile X position</param>
	/// <param name="y">Ground tile Y position</param>
	/// <returns></returns>
	public virtual bool TryGenerate(int x, int y)
	{
		return false;
	}

	public virtual int GetStyle(int x, int y) => 0;

	/// <summary>
	/// Gets texture coordinates data for custom top texture
	/// </summary>
	public virtual bool GetTreeFoliageData(int x, int y, int xoffset, ref int treeFrame, out int floorY, out int topTextureFrameWidth, out int topTextureFrameHeight)
	{
		int v = 0;
		return WorldGen.GetCommonTreeFoliageData(x, y, xoffset, ref treeFrame, ref v, out floorY, out topTextureFrameWidth, out topTextureFrameHeight);
	}

	/// <summary>
	/// Gets foliage assets for tree tops and branches for each texture style<br/>
	/// Texture style is received from <see cref="GetStyle"/><br/>
	/// This method is called only once for each style
	/// </summary>
	/// <param name="style"></param>
	/// <param name="branch"></param>
	/// <returns></returns>
	public virtual Asset<Texture2D> GetFoliageTexture(int style, bool branch)
	{
		if (branch)
			return ModContent.Request<Texture2D>(BranchTexture);
		else
			return ModContent.Request<Texture2D>(TopTexture);
	}

	public Asset<Texture2D> GetFoliageTextureCached(int style, bool branch)
	{
		int cacheId = style * 2 + (branch ? 1 : 0);
		while (cacheId >= FoliageTextureCache.Count)
			FoliageTextureCache.Add(null);

		if (FoliageTextureCache[cacheId] is null) {
			FoliageTextureCache[cacheId] = GetFoliageTexture(style, branch);
		}

		return FoliageTextureCache[cacheId];
	}

	protected override void Register()
	{
		TreeStyle = CustomTreeLoader.NextTreeStyle();

		Tile ??= new CustomTreeDefaultTile();
		Tile.Tree = this;
		Mod.AddContent(Tile);
		CustomTreeLoader.byTileLookup[Tile.Type] = this;

		Sapling  ??= new CustomTreeDefaultSapling();
		Sapling.Tree = this;
		Mod.AddContent(Sapling);
		CustomTreeLoader.bySaplingLookup[Sapling.Type] = this;

		Acorn ??= new CustomTreeDefaultAcorn();
		Acorn.Tree = this;
		Mod.AddContent(Acorn);
		CustomTreeLoader.byAcornLookup[Acorn.Type] = this;

		if (Leaf is null && LeafTexture is not null)
			Leaf = new CustomTreeDefaultLeaf();

		if (Leaf is not null)
		{
			Leaf.Tree = this;
			Mod.AddContent(Leaf);
			CustomTreeLoader.byCustomLeafLookup[Leaf.Type] = this;
		}

		CustomTreeLoader.trees.Add(this);
	}

	public override void SetupContent() => SetStaticDefaults();
}

[Autoload(false)]
public abstract class CustomTreeTile : ModTile
{
	private ModCustomTree tree = null;
	public ModCustomTree Tree { get => tree ?? CustomTreeLoader.GetByTile(Type); internal protected set => tree = value; }

	public override string Texture => Tree.TileTexture;

	public LocalizedText DefaultMapNameLocalization => Tree.GetLocalization("TileMapName", Tree.PrettyPrintName);

	public override void SetStaticDefaults()
	{
		Main.tileAxe[Type] = true;
		Main.tileFrameImportant[Type] = true;

		TileID.Sets.IsATreeTrunk[Type] = true;
		TileID.Sets.IsShakeable[Type] = true;
		TileID.Sets.GetsDestroyedForMeteors[Type] = true;
		TileID.Sets.GetsCheckedForLeaves[Type] = true;
		TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
		TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;
	}

	public override void SetDrawPositions(int x, int y, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
	{
		width = 20;
		height = 20;
	}

	public override bool TileFrame(int x, int y, ref bool resetFrame, ref bool noBreak)
	{
		CustomTreeGenerator.CheckTree(x, y, Tree.GetTreeSettings());
		return false;
	}
}

[Autoload(false)]
public abstract class CustomTreeSapling : ModTile
{
	private ModCustomTree tree = null;
	public ModCustomTree Tree { get => tree ?? CustomTreeLoader.GetBySapling(Type); internal protected set => tree = value; }

	public override string Texture => Tree.SaplingTexture;

	public LocalizedText DefaultMapNameLocalization => Tree.GetLocalization("SaplingMapName", () => Tree.PrettyPrintName() + " Sapling");

	public override void SetStaticDefaults()
	{
		static bool HookCanPlace(ushort type, int x, int y, int style, int dir, int alternate)
		{
			ModCustomTree tree = CustomTreeLoader.GetBySapling(type);
			if (tree is null)
				return true;
			else
				return tree.ValidWallType(Framing.GetTileSafely(x, y).WallType) && tree.ValidGroundType(Framing.GetTileSafely(x, y + 1).TileType);
		}

		TileID.Sets.TreeSapling[Type] = true;
		TileID.Sets.CommonSapling[Type] = true;
		TileID.Sets.SwaysInWindBasic[Type] = true;

		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;

		TileObjectData.newTile.Width = 1;
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.HookCheckIfCanPlace = new((x, y, _, style, dir, alt) => HookCanPlace(Type, x, y, style, dir, alt) ? 1 : 0, 0, 0, true);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.DrawFlipHorizontal = true;
		TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
		TileObjectData.newTile.LavaDeath = false;
		TileObjectData.newTile.RandomStyleRange = Tree.SaplingStyles;
		TileObjectData.newTile.StyleMultiplier = 1;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.addTile(Type);
	}

	public override bool TileFrame(int x, int y, ref bool resetFrame, ref bool noBreak)
	{
		WorldGen.Check1x2(x, y, Type);
		return false;
	}

	public override void RandomUpdate(int x, int y)
	{
		ModCustomTree tree = Tree;
		if (Main.rand.Next(tree.GrowChance) == 0)
			tree.Grow(x, y);
	}
}

[Autoload(false)]
public abstract class CustomTreeAcorn : ModItem
{
	private ModCustomTree tree = null;
	public ModCustomTree Tree { get => tree ?? CustomTreeLoader.GetByAcorn(Type); internal protected set => tree = value; }

	public override string Texture => Tree.AcornTexture;

	public LocalizedText DefaultDisplayNameLocalization => Tree.GetLocalization("AcornDisplayName", () => Tree.PrettyPrintName() + " Acorn");
	public LocalizedText DefaultTooltipLocalization => Tree.GetLocalization("AcornTooltip", () => "");

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(Tree.Sapling.Type);
	}
}

[Autoload(false)]
public abstract class CustomTreeLeaf : ModGore
{
	private ModCustomTree tree = null;
	public ModCustomTree Tree { get => tree ?? CustomTreeLoader.GetByCustomLeaf(Type); internal protected set => tree = value; }

	public override string Texture => Tree.LeafTexture ?? base.Texture;

	public override void SetStaticDefaults()
	{
		ChildSafety.SafeGore[Type] = true;
		GoreID.Sets.SpecialAI[Type] = 3;
		GoreID.Sets.PaintedFallingLeaf[Type] = true;
	}
}
