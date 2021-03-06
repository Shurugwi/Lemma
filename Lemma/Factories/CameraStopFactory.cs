﻿using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class CameraStopFactory : Factory<Main>
	{
		public CameraStopFactory()
		{
			this.Color = new Vector3(0.4f, 1.5f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "CameraStop");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			CameraStop cameraStop = entity.GetOrCreate<CameraStop>("CameraStop");

			entity.CannotSuspendByDistance = true;

			this.SetMain(entity, main);

			VoxelAttachable.MakeAttachable(entity, main).EditorProperties();
			
			if (main.EditorEnabled)
			{
				entity.Add("Preview", new Command
				{
					Action = delegate()
					{
						ulong id = entity.GUID;

						Action go = delegate()
						{
							main.EditorEnabled.Value = false;
							IO.MapLoader.Load(main, main.MapFile);

							main.Spawner.CanSpawn = false;
							main.Renderer.Brightness.Value = 0.0f;
							main.Renderer.InternalGamma.Value = 0.0f;
							main.UI.IsMouseVisible.Value = false;
							
							main.AddComponent(new PostInitialization(delegate()
							{
								// We have to squirrel away the ID and get a new entity
								// because OUR entity got wiped out by the MapLoader.
								main.GetByGUID(id).Get<CameraStop>().Go.Execute();
							}));
						};

						Editor editor = main.Get("Editor").First().Get<Editor>();
						if (editor.NeedsSave)
							editor.SaveWithCallback(go);
						else
							go();
					},
				}, Command.Perms.Executable);
			}

			entity.Add("Go", cameraStop.Go);
			entity.Add("OnDone", cameraStop.OnDone);
			entity.Add("Offset", cameraStop.Offset);
			entity.Add("Blend", cameraStop.Blend);
			entity.Add("Duration", cameraStop.Duration);
			entity.Add("FieldOfView", cameraStop.FieldOfView);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "AlphaModels\\light";
			model.Color.Value = this.Color;
			model.Serialize = false;
			model.Scale.Value = new Vector3(1, 1, -1);
			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
			entity.Add("EditorModel", model);
			model.Add(new Binding<bool>(model.Enabled, Editor.EditorModelsVisible));

			VoxelAttachable.AttachEditorComponents(entity, main);

			ModelAlpha offsetModel = new ModelAlpha();
			offsetModel.Filename.Value = "AlphaModels\\cone";
			offsetModel.Add(new Binding<Vector3>(offsetModel.Color, model.Color));

			CameraStop cameraStop = entity.Get<CameraStop>();

			offsetModel.Add(new Binding<bool>(offsetModel.Enabled, () => entity.EditorSelected && cameraStop.Offset != 0 && Editor.EditorModelsVisible, entity.EditorSelected, cameraStop.Offset, Editor.EditorModelsVisible));
			offsetModel.Add(new Binding<Vector3, float>(offsetModel.Scale, x => new Vector3(1, 1, x), cameraStop.Offset));
			offsetModel.Add(new Binding<Matrix>(offsetModel.Transform, model.Transform));
			offsetModel.Serialize = false;
			entity.Add("EditorModel3", offsetModel);

			EntityConnectable.AttachEditorComponents(entity, "Next", cameraStop.Next);
		}
	}
}