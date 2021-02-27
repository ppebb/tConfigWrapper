using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using System;
using tConfigWrapper.Common;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Chat;
using Microsoft.Xna.Framework.Graphics;

namespace tConfigWrapper.UI {
	public class UITextBox : UITextPanel<string> { // Lmao most of this is ~~stolen~~ borrowed from Jopojelly
		private readonly Vector2 _position;
		private readonly string _hintText;
		private readonly string _hoverText;
		private int _frameCount;
		private int _cursor;
		internal bool focused = false;
		private readonly bool _centerAroundLine;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="size"></param>
		/// <param name="hintText"></param>
		/// <param name="hoverText"></param>
		/// <param name="textScale"></param>
		/// <param name="large">This bool is very broken, just leave it false</param>
		public UITextBox(Vector2 position, Vector2 size, string hintText, string hoverText, bool centerAroundLine = false, float textScale = 1, bool large = false) : base("", textScale, large) {
			_position = position;
			_hintText = hintText;
			_hoverText = hoverText;
			Width.Pixels = size.X + 20;
			Height.Pixels = size.Y / 2;
			Top.Pixels = position.Y;
			Left.Pixels = position.X;
			_centerAroundLine = centerAroundLine;
			if (centerAroundLine)
				Left.Pixels = _position.Y - Width.Pixels / 2;
			SetPadding(4);
		}

		public override void Click(UIMouseEvent evt) {
			base.Click(evt);
			FocusTextBox();
		}

		public void FocusTextBox() {
			if (!focused) {
				Main.clrInput();
				focused = true;
				Main.blockInput = true;
			}
		}

		public void UnfocusTextBox() {
			if (focused) {
				focused = false;
				Main.blockInput = false;
			}
		}

		public override void Update(GameTime gameTime) {
			Vector2 MousePosition = new Vector2(Main.mouseX, Main.mouseY);

			Vector2 hintSize = Main.fontMouseText.MeasureString(_hintText);
			Vector2 textSize = Main.fontMouseText.MeasureString(Text);

			Width.Pixels = hintSize.X > textSize.X ? hintSize.X + 17 : textSize.X + 17;
			Height.Pixels = textSize.Y / 2;

			if (_centerAroundLine)
				Left.Pixels = _position.Y - Width.Pixels / 2;


			if (!ContainsPoint(MousePosition) && Main.mouseLeft)
				UnfocusTextBox();

			base.Update(gameTime);
		}


		public void Write(string text) {
			SetText(Text.Insert(_cursor, text));
			_cursor += text.Length;
			_cursor = Math.Min(Text.Length, _cursor);
			Recalculate();
		}

		public void WriteAll(string text) {
			bool changed = text != Text;
			SetText(text);
			_cursor = text.Length;
			Recalculate();
		}

		public override void SetText(string text, float textScale, bool large) {
			base.SetText(text, textScale, large);

			_cursor = Math.Min(Text.Length, _cursor);
		}

		public void BackSpace() {
			if (_cursor == 0)
				return;

			SetText(Text.Substring(0, Text.Length - 1));
			Recalculate();
		}

		public void CursorLeft() {
			if (_cursor == 0)
				return;

			_cursor--;
		}

		public void CursorRight() {
			if (_cursor < Text.Length)
				_cursor++;
		}

		private static bool JustPressed(Keys key) => Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			Rectangle hitbox = GetDimensions().ToRectangle();
			Main.spriteBatch.Draw(Main.magicPixel, hitbox, Color.White);

			if (focused) {
				PlayerInput.WritingText = true;
				Main.instance.HandleIME();
				WriteAll(Main.GetInputText(Text));

				if (JustPressed(Keys.Escape))
					UnfocusTextBox();

				if (JustPressed(Keys.Enter)) {
					ModState.SerializeModPack(Text);
					SetText("");
					Recalculate();
					UnfocusTextBox();
				}

				if (JustPressed(Keys.Up))
					_cursor = 0;

				if (JustPressed(Keys.Down))
					_cursor = Text.Length;

				if (JustPressed(Keys.Left))
					CursorLeft();

				if (JustPressed(Keys.Right))
					CursorRight();

				if (JustPressed(Keys.Back))
					BackSpace();

				if (!hitbox.Contains(Main.MouseScreen.ToPoint()) && Main.mouseLeft)
					UnfocusTextBox();
			}



			CalculatedStyle innerDimensions = base.GetInnerDimensions();
			Vector2 pos = innerDimensions.Position();
			DynamicSpriteFont spriteFont = base.IsLarge ? Main.fontDeathText : Main.fontMouseText;
			Vector2 vector = new Vector2(spriteFont.MeasureString(base.Text.Substring(0, this._cursor)).X, base.IsLarge ? 32f : 16f) * base.TextScale;

			if (Text.Length == 0) {
				Vector2 hintTextSize = new Vector2(spriteFont.MeasureString(_hintText).X, IsLarge ? 32f : 16f) * TextScale;
				pos.X += 5;//(hintTextSize.X);
				Utils.DrawBorderString(spriteBatch, _hintText, pos, Color.Gray, base.TextScale, 0f, 0f, -1);
				pos.X -= 5;
				//pos.X -= (innerDimensions.Width - hintTextSize.X) * 0.5f;
			}
			else
				spriteBatch.DrawString(Main.fontMouseText, Text, pos, Color.Black);

			_frameCount++;

			if (!focused)
				return;

			if (hitbox.Contains(Main.MouseScreen.ToPoint()))
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Main.fontMouseText, _hoverText, new Vector2(Main.MouseScreen.X + 24, Main.MouseScreen.Y), Color.White, 0f, Vector2.Zero, Vector2.One);

			pos.X += /*(innerDimensions.Width - base.TextSize.X) * 0.5f*/ +vector.X - (base.IsLarge ? 8f : 4f) * base.TextScale + 6f;
			if ((_frameCount %= 40) > 20) {
				return;
			}
			Utils.DrawBorderString(spriteBatch, "|", pos, base.TextColor, base.TextScale, 0f, 0f, -1);
		}
	}
}