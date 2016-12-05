using System;
using Verse.AI;
using RimWorld;

namespace Verse
{
	/// <summary>
	/// Extends <see cref="Pawn"/> to add separate set of sprites for movement.
	/// </summary>
	public class AnimatedMovementPawn : Pawn
	{
		/// <summary> The movement graphic for the current life stage </summary>
		private Graphic movementGraphic;

		/// <summary> Tracks current movement state to see when it changed </summary>
		private bool wasMovingBefore = false;

		/// <summary> Tracks the current life stage to see when it changed </summary>
		private PawnKindLifeStage lifeStageBefore;

		/// <summary> Main constructor </summary>
		public AnimatedMovementPawn() : base() { }

		/// <summary> Update the graphic if the life stage changed and swap the body graphic if moving </summary>
		public override void Tick()
		{
			base.Tick();

			if (lifeStageBefore != ageTracker.CurKindLifeStage || movementGraphic == null)
			{
				UpdateGraphic();
				lifeStageBefore = ageTracker.CurKindLifeStage;
			}
			if (pather.MovingNow && !wasMovingBefore)
			{
				Drawer.renderer.graphics.nakedGraphic = movementGraphic;
				Drawer.renderer.graphics.ClearCache();
				wasMovingBefore = true;
			}
			else if (!pather.MovingNow && wasMovingBefore)
			{
				Drawer.renderer.graphics.nakedGraphic = ageTracker.CurKindLifeStage.bodyGraphicData.Graphic;
				Drawer.renderer.graphics.ClearCache();
				wasMovingBefore = false;
			}
		}

		/// <summary> Set the movementGraphic based on the current life stage </summary>
		private void UpdateGraphic()
		{
			var data = new GraphicData();
			data.CopyFrom(ageTracker.CurKindLifeStage.bodyGraphicData);
			data.texPath = data.texPath + "_moving";
			movementGraphic = data.Graphic;
		}
	}
}
