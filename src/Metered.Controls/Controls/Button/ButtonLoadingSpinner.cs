namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="Button"/>'s <c>LoadingSpinner</c> part: Base Web's spinning <c>span</c>, the ring
/// <see cref="SpinnerRing"/> draws, sized by its <c>Width</c> and <c>Height</c> (Base Web's <c>box-sizing:
/// border-box</c>), with <see cref="SpinnerRing.Thickness"/> Base Web's <c>sizing.scale0</c>, 2. Its theme,
/// <c>Button.StyledLoadingSpinner</c>, colours it by kind and turns it.
/// </summary>
public class ButtonLoadingSpinner : SpinnerRing
{
}
