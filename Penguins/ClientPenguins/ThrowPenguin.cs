using Penguins.ClientPenguins;
using Waddle.Config;
using Waddle.Config.Exceptions;

#region ReadmeInfo
// Throws an error. Either `ifTruthy` or `ifFalsy` can be set to variable names to only throw if the variable has a falsy ("0", "false", "null", unset) or truthy (anything else) value (case-insensitive, whitespace will be trimmed)
// `Error` (string), `ifTruthy` (string), `ifFalsy` (string)
#endregion

namespace Penguins
{
    internal partial class PenguinMatcher
    {
        [PenguinMapping(["Error", "IfTruthy", "IfFalsy"], "ThrowPenguin")]
        public IPenguin MatchThrowPenguin(YamlPenguin yp)
        {
            return yp switch
            {
                { Error: not null, IfTruthy: not null, IfFalsy: not null } =>
                    throw new InvalidPenguinException(
                        "ThrowPenguin must not define both `IfTruthy` and `IfFalsy`"
                    ),
                { Error: string error, IfTruthy: string ifTruthy } => new ThrowPenguin(context)
                {
                    Name = yp.Name,
                    Error = error,
                    Variable = ifTruthy,
                    IfTruthy = true,
                },
                { Error: string error, IfFalsy: string ifFalsy } => new ThrowPenguin(context)
                {
                    Name = yp.Name,
                    Error = error,
                    Variable = ifFalsy,
                    IfTruthy = false,
                },
                { Error: string error } => new ThrowPenguin(context)
                {
                    Name = yp.Name,
                    Error = error
                },
                _ => throw new NoMatchException()
            };
        }
    }
}

namespace Penguins.ClientPenguins
{
    public class ThrowPenguin(WaddleContext context) : PenguinBase(context)
    {
        public required string Error;
        public string? Variable;
        public bool IfTruthy;

        private static readonly string[] _falsy = ["0", "false", "null"];

        public override async Task Execute(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Variable))
            {
                throw new PenguinCustomException(Error);
            }

            bool success = _context.Variables.TryGetValue(Variable, out string? value);
            // Cases
            // IfTruthy   falsy    should throw
            // true       true     no
            // false      false    no
            // true       false    yes
            // false      true     yes
            bool falsy = !success || value is null || _falsy.Contains(value.Trim().ToLower());
            if (IfTruthy && falsy) // case 1
            {
                return;
            }
            if (!IfTruthy && !falsy) // case 2
            {
                return;
            }
            if (IfTruthy && !falsy) // case 3
            {
                throw new PenguinCustomException(Error);
            }
            if (!IfTruthy && falsy) // case 4
            {
                throw new PenguinCustomException(Error);
            }
        }
    }
}
