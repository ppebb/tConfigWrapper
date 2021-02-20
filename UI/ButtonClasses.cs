using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria.UI;
using Terraria.UI.Chat;

namespace tConfigWrapper.UI {
	public class GenericButton : UIElement {
		private readonly Func<string> _textToDisplay;
		private readonly Vector2 _position;
		private readonly Vector2 _scale;
		private readonly Action _action;
		private readonly Func<Color> _color;
		private readonly DynamicSpriteFont _font;

		public GenericButton(Func<string> textToDisplay, Vector2 position, Vector2 scale, Action action, Func<Color> color, DynamicSpriteFont font) {
			_textToDisplay = textToDisplay;
			_position = position;
			_scale = scale;
			_action = action;
			_color = color;
			_font = font;
			Vector2 size = font.MeasureString(textToDisplay.Invoke());
			Width.Pixels = size.X;
			Height.Pixels = size.Y - 6;
			Top.Pixels = position.Y;
			Left.Pixels = position.X;
		}

		public override void Draw(SpriteBatch spriteBatch) {
			//spriteBatch.Draw(Main.magicPixel, GetDimensions().ToRectangle(), null, Color.Black);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, _font, _textToDisplay.Invoke(), _position, _color.Invoke(), 0f, Vector2.Zero, _scale);
			//spriteBatch.DrawString(_font, _textToDisplay.Invoke(), _position, _color.Invoke());
		}

		public override void Click(UIMouseEvent evt) {
			base.Click(evt);
			_action.Invoke();
		}
	}
}