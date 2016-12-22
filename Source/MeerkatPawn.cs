using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;

namespace Verse
{
	/// <summary>
	/// Extends <see cref="Pawn"/> to add separate set of sprites for movement.
	/// </summary>
	public class MeerkatPawn : Pawn
	{
		/// <summary> List of graphic replacements </summary>
		private List<GraphicReplacement> replacements = new List<GraphicReplacement>();

		/// <summary> Tracks the current life stage to see when it changed </summary>
		private PawnKindLifeStage lifeStageBefore;

		/// <summary> Cache ModContentHolder for this mod </summary>
		private ModContentHolder<Texture2D> contentHolder = LoadedModManager.RunningMods
			.FirstOrDefault(x => x.Name == "MeerkatMod")
			.GetContentHolder<Texture2D>();

		private bool IsMoving => pather.Moving;
		private bool IsFighting => CurJob?.def == JobDefOf.AttackMelee;

		/// <summary> Initialize the graphic replacers </summary>
		private void SetupReplacements()
		{
			replacements.Clear();
			AddReplacement(LoadGraphic("moving"), () => IsMoving || IsFighting);
			AddReplacement(ageTracker.CurKindLifeStage.bodyGraphicData.Graphic, () => true); // default graphic if no previous match
		}

		private void AddReplacement(Graphic graphic, System.Func<bool> condition)
		{
			if (graphic == null) return;
			replacements.Add(new GraphicReplacement(graphic, condition));
		}

		/// <summary> Set the movementGraphic based on the current life stage </summary> 
		private Graphic LoadGraphic(string variant)
		{
			string path = ageTracker.CurKindLifeStage.bodyGraphicData.texPath + "_" + variant;

			// try getting the texture to see if it exists
			var texture = contentHolder.Get(path + "_front");
			if (texture == null) return null;

			var data = new GraphicData();
			data.CopyFrom(ageTracker.CurKindLifeStage.bodyGraphicData);
			data.texPath = path;
			return data.Graphic;
		}

		/// <summary> Update the graphic if the life stage changed and swap the body graphic if moving </summary>
		public override void Tick()
		{
			base.Tick();

			if (pather == null) return; // this can occur if the pawn leaves the map area

			// initialize the replacement graphics (once per lifestage)
			if (lifeStageBefore != ageTracker.CurKindLifeStage)
			{
				SetupReplacements();
				lifeStageBefore = ageTracker.CurKindLifeStage;
			}

			// avoid hunting tamed animals, accept non-ideal food instead
			if (Faction != Faction.OfPlayer && jobs.curJob.def == JobDefOf.PredatorHunt)
			{
				if ((jobs.curJob.targetA.Thing as Pawn)?.Faction == Faction.OfPlayer)
				{
					jobs.StopAll(); // stop predator hunt job

					if (needs.food.TicksStarving < 30000)
					{
						// find food
						var food = GenClosest.ClosestThingReachable(Position, Map,
							ThingRequest.ForGroup(ThingRequestGroup.HaulableAlways),
							AI.PathEndMode.OnCell,
							TraverseParms.For(this, Danger.Deadly, TraverseMode.ByPawn, false),
							maxDistance: 100f,
							validator: (thing) =>
								thing.def.category == ThingCategory.Item &&
								thing.def.IsNutritionGivingIngestible
								/*&& RaceProps.CanEverEat(thing)*/, // omit food preferences
							customGlobalSearchSet: null,
							searchRegionsMax: -1,
							forceGlobalSearch: false);

						if (food != null)
						{
							// try to find other food
							jobs.StartJob(new AI.Job(JobDefOf.Ingest, food));
						}
						else
						{
							// wander until food is found or too starving to continue
							StartJobWander();
						}
					}
					else // if desperate
					{
						IntVec3 exit_dest;
						if (RCellFinder.TryFindBestExitSpot(this, out exit_dest, TraverseMode.ByPawn))
						{
							// exit map
							jobs.StartJob(new AI.Job(JobDefOf.Goto, exit_dest) { exitMapOnArrival = true });
						}
						else
						{
							// fallback to wandering
							StartJobWander();
						}
					}
				}
			}

			// attempt to find a replacement graphic
			foreach (var replacement in replacements)
			{
				if (replacement.TryReplace(Drawer.renderer))
					break; // break on first successful replacement
			}
		}

		/// <summary> Starts an idle job. </summary>
		private void StartJobWander()
		{
			IntVec3 wander_dest = RCellFinder.RandomWanderDestFor(this, Position, 12f, null, Danger.Deadly);
			if (wander_dest.IsValid)
			{
				// wander
				jobs.StartJob(new AI.Job(JobDefOf.GotoWander, wander_dest) { expiryInterval = 1500 });
			}
			else
			{
				// wait
				jobs.StartJob(new AI.Job(JobDefOf.WaitWander) { expiryInterval = 1500 });
			}
		}
	}
}
