using System.Text;
using Spectre.Console;

namespace TinkNet;

public static class ReplPrompt
{
    private static List<string> _history = new List<string>();

    public static string ReadCode()
    {
        AnsiConsole.Markup("[bold blue]>[/] ");
        
        var input = new StringBuilder();
        int cursorPos = 0;
        int historyIndex = _history.Count;
        string uncommittedInput = "";

        int startLeft = Console.CursorLeft;
        int startTop = Console.CursorTop;
        int lastInputLength = 0;

        void Redraw()
        {
            Console.SetCursorPosition(startLeft, startTop);

            // Write the current input
            var currentInput = input.ToString();
            Console.Write(currentInput);

            // Erase any leftover characters if the string got shorter
            if (currentInput.Length < lastInputLength)
            {
                Console.Write(new string(' ', lastInputLength - currentInput.Length));
            }
            lastInputLength = currentInput.Length;

            // Move cursor to the correct logical position
            int newLeft = startLeft + cursorPos;
            int newTop = startTop;

            while (newLeft >= Console.BufferWidth)
            {
                newLeft -= Console.BufferWidth;
                newTop++;
            }

            Console.SetCursorPosition(newLeft, newTop);
        }

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    var result = input.ToString();
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        // Avoid adding duplicate consecutive commands
                        if (_history.Count == 0 || _history.Last() != result)
                        {
                            _history.Add(result);
                        }
                    }
                    return result;

                case ConsoleKey.UpArrow:
                    if (historyIndex > 0)
                    {
                        if (historyIndex == _history.Count) 
                        {
                            uncommittedInput = input.ToString();
                        }
                        historyIndex--;
                        input.Clear().Append(_history[historyIndex]);
                        cursorPos = input.Length;
                        Redraw();
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (historyIndex < _history.Count)
                    {
                        historyIndex++;
                        input.Clear();
                        if (historyIndex == _history.Count)
                        {
                            input.Append(uncommittedInput);
                        }
                        else
                        {
                            input.Append(_history[historyIndex]);
                        }
                        cursorPos = input.Length;
                        Redraw();
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (cursorPos > 0)
                    {
                        cursorPos--;
                        Redraw();
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (cursorPos < input.Length)
                    {
                        cursorPos++;
                        Redraw();
                    }
                    break;

                case ConsoleKey.Backspace:
                    if (cursorPos > 0)
                    {
                        input.Remove(cursorPos - 1, 1);
                        cursorPos--;
                        Redraw();
                    }
                    break;

                case ConsoleKey.Delete:
                    if (cursorPos < input.Length)
                    {
                        input.Remove(cursorPos, 1);
                        Redraw();
                    }
                    break;

                case ConsoleKey.Home:
                    cursorPos = 0;
                    Redraw();
                    break;

                case ConsoleKey.End:
                    cursorPos = input.Length;
                    Redraw();
                    break;

                default:
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        input.Insert(cursorPos, keyInfo.KeyChar);
                        cursorPos++;
                        Redraw();
                    }
                    break;
            }
        }
    }
}