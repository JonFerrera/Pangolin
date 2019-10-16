using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Pangolin
{
    static class RegularExpressionLayer
    {
        private const int _maxPathLength = 260;

        private const RegexOptions _regexOptions = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled;

        private const bool _rxPublic = true;

        private static readonly TimeSpan _matchTimeout = new TimeSpan(0, 0, 0, 10, 0);

        private const char _censorNumberCharacter = '#';

        private const string _space = " ";

        private static readonly Regex _betweenAngleBrackets = new Regex(@"\<([^)]+)\>", _regexOptions, _matchTimeout);
        private static readonly Regex _betweenCurlyBraces = new Regex(@"\{([^)]+)\}>", _regexOptions, _matchTimeout);
        private static readonly Regex _betweenParentheses = new Regex(@"\(([^)]+)\)", _regexOptions, _matchTimeout);
        private static readonly Regex _betweenQuotations = new Regex("([\"'])(?:(?=(\\\\?))\\2.)*?\\1", _regexOptions, _matchTimeout);
        private static readonly Regex _betweenQuotationsDouble = new Regex("([\"])(?:(?=(\\\\?))\\2.)*?\\1", _regexOptions, _matchTimeout);
        private static readonly Regex _betweenQuotationsSingle = new Regex("(['])(?:(?=(\\\\?))\\2.)*?\\1", _regexOptions, _matchTimeout);

        private static readonly Regex _creditCardNumber = new Regex("^(?:4[0-9]{12}(?:[0-9]{3})?|(?:5[1-5][0-9]{2}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{2}|27[01][0-9]|2720)[0-9]{12}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|6(?:011|5[0-9]{2})[0-9]{12}|(?:2131|1800|35\\d{3})\\d{11})$", _regexOptions, _matchTimeout);
        private static readonly Regex _creditCardNumberAmericanExpress = new Regex("^3[47][0-9]{13}$", _regexOptions, _matchTimeout);
        private static readonly Regex _creditCardNumberDinersClub = new Regex("^3(?:0[0-5]|[68][0-9])[0-9]{11}$", _regexOptions, _matchTimeout);
        private static readonly Regex _creditCardNumberDiscover = new Regex("^6(?:011|5[0-9]{2})[0-9]{12}$", _regexOptions, _matchTimeout);
        private static readonly Regex _creditCardNumberMasterCard = new Regex("^(?:5[1-5][0-9]{2}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{2}|27[01][0-9]|2720)[0-9]{12}$", _regexOptions, _matchTimeout);
        private static readonly Regex _creditCardNumberJCB = new Regex("^(?:2131|1800|35\\d{3})\\d{11}$", _regexOptions, _matchTimeout);
        private static readonly Regex _creditCardNumberVisa = new Regex("^4[0-9]{12}(?:[0-9]{3})?$", _regexOptions, _matchTimeout);
        private static readonly Regex _cardSecurityCode = new Regex("^[0-9]{3,4}$", _regexOptions, _matchTimeout);

        private static readonly Regex _email = new Regex("^(?(\")(\".+?(?<!\\\\)\"@)|(([0-9a-z]((\\.(?!\\.))|[-!#\\$%&'\\*\\+/=\\?\\^`\\{\\}\\|~\\w])*)(?<=[0-9a-z])@))(?(\\[)(\\[(\\d{1,3}\\.){3}\\d{1,3}\\])|(([0-9a-z][-\\w]*[0-9a-z]*\\.)+[a-z0-9][\\-a-z0-9]{0,22}[a-z0-9]))$", _regexOptions, _matchTimeout); // http://emailregex.com/
        private static readonly Regex _emailRFC5322 = new Regex("(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])", _regexOptions, _matchTimeout); // http://emailregex.com/
        private static readonly Regex _emailHtml5 = new Regex("^[a-z0-9.!#$%&’*+/=?^_`{|}~-]+@[a-z0-9-]+(?:\\.[a-z0-9-]+)*$", _regexOptions, _matchTimeout); // http://emailregex.com/
        
        private static readonly Regex _hashtag = new Regex("\\B#[a-z_]+[a-z0-9_]*\\b", _regexOptions, _matchTimeout);
        private static readonly Regex _hashtagStockTicker = new Regex("\\B\\$[a-z]+(?:[\\.-]([a-z]|(pk|sc|nm)))?\\b", _regexOptions, _matchTimeout);
        private static readonly Regex _hexColor = new Regex("^(?=[a-fA-F0-9]*$)(?:.{8}|.{6}|.{3})$", _regexOptions, _matchTimeout);
        private static readonly Regex _htmlTag = new Regex("<([a-z][a-z0-9]*)\b[^>]*>(.*?)</\\1>", _regexOptions, _matchTimeout);
        private static readonly Regex _ipv4 = new Regex("^(([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.){3}([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$", _regexOptions, _matchTimeout);
        private static readonly Regex _ipv6 = new Regex("^([a-fA-F0-9]{1,4}\\:){1,7}\\:?([a-fA-F0-9]{1,4}\\:){0,6}([a-fA-F0-9]{1,4})$", _regexOptions, _matchTimeout);

        private static readonly Regex _password = new Regex("^(?.*[a-z])(?.*[A-Z])(?.*[0-9])(?.*[!@#\\$%\\^&\\*])(?.[15,])$", _regexOptions, _matchTimeout);
        private static readonly Regex _phoneNumber = new Regex("^[0-9]{0,14}$", _regexOptions, _matchTimeout);
        private static readonly Regex _slug = new Regex("^[a-zA-Z0-9-]+$", _regexOptions, _matchTimeout);
        private static readonly Regex _socialSecurityNumber = new Regex("^([0-9]{3}[-]?[0-9]{2}[-]?[0-9]{4})$", _regexOptions, _matchTimeout);
        private static readonly Regex _statesUs = new Regex("^(a[aelkprsz]|c[aot]|d[ce]|f[lm]|g[au]|hi|i[adln]|k[sy]|la|m[adehinopst]|n[cdehjmvy]|o[hkr]|p[arw]|ri|s[cd]|t[nx]|ut|v[ait]|w[aivy])$", _regexOptions, _matchTimeout);

        private static readonly Regex _url = new Regex("^(?:(?:https?|ftp):\\/\\/)(?:\\S+(?::\\S*)?@)?(?:(?!(?:10|127)(?:\\.\\d{1,3}){3})(?!(?:169\\.254|192\\.168)(?:\\.\\d{1,3}){2})(?!172\\.(?:1[6-9]|2\\d|3[0-1])(?:\\.\\d{1,3}){2})(?:[1-9]\\d?|1\\d\\d|2[01]\\d|22[0-3])(?:\\.(?:1?\\d{1,2}|2[0-4]\\d|25[0-5])){2}(?:\\.(?:[1-9]\\d?|1\\d\\d|2[0-4]\\d|25[0-4]))|(?:(?:[a-z\\u00a1-\\uffff0-9]-*)*[a-z\\u00a1-\\uffff0-9]+)(?:\\.(?:[a-z\\u00a1-\\uffff0-9]-*)*[a-z\\u00a1-\\uffff0-9]+)*(?:\\.(?:[a-z\\u00a1-\\uffff]{2,}))\\.?)(?::\\d{2,5})?(?:[/?#]\\S*)?$", _regexOptions, _matchTimeout); // https://gist.github.com/dperini/729294
        private static readonly Regex _username = new Regex("^[a-z0-9_+%-.]{1,50}$", _regexOptions, _matchTimeout);
        private static readonly Regex _usernameSocial = new Regex("^@[a-z0-9_+%-.]{1,50}$", _regexOptions, _matchTimeout);
        private static readonly Regex _whitespace = new Regex("\\s+", _regexOptions, _matchTimeout);
        private static readonly Regex _zipCodeUs = new Regex("^\\d{5}(?:[-\\s]\\d{4})?", _regexOptions, _matchTimeout);

        private static bool GetIsMatch(Match match)
        {
            return match != null ? match.Success : false;
        }
        private static string GetMatch(Match match)
        {
            return match != null && match.Success ? match.Value : string.Empty;
        }
        private static string[] GetMatches(Match match)
        {
            List<string> matchList = new List<string>();

            if (match == null)
            {
                return matchList.ToArray();
            }

            try
            {
                while (match.Success)
                {
                    matchList.Add(match.Value);
                    match = match.NextMatch();
                }
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
            }

            return matchList.ToArray();
        }

        #region Between Special Characters
        public static string GetAngleBracketsText(string input)
        {
            Match match = GetAngleBracketsMatch(input);
            return GetMatch(match);
        }
        public static string[] GetAngleBracketsTextAll(string input)
        {
            Match match = GetAngleBracketsMatch(input);
            return GetMatches(match);
        }
        public static bool IsAngleBracketsMatch(string input)
        {
            Match match = GetAngleBracketsMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetAngleBracketsMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _betweenAngleBrackets.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static string GetCurlyBracesText(string input)
        {
            Match match = GetCurlyBracesMatch(input);
            return GetMatch(match);
        }
        public static string[] GetCurlyBracesTextAll(string input)
        {
            Match match = GetCurlyBracesMatch(input);
            return GetMatches(match);
        }
        public static bool IsCurlyBracesMatch(string input)
        {
            Match match = GetCurlyBracesMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetCurlyBracesMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _betweenCurlyBraces.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static string GetParenthesesText(string input)
        {
            Match match = GetParenthesesMatch(input);
            return GetMatch(match);
        }
        public static string[] GetParenthesesTextAll(string input)
        {
            Match match = GetParenthesesMatch(input);
            return GetMatches(match);
        }
        public static bool IsParenthesesMatch(string input)
        {
            Match match = GetParenthesesMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetParenthesesMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _betweenParentheses.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static string GetQuotationsText(string input)
        {
            Match match = GetQuotationsMatch(input);
            return GetMatch(match);
        }
        public static string[] GetQuotationsTextAll(string input)
        {
            Match match = GetQuotationsMatch(input);
            return GetMatches(match);
        }
        public static bool IsQuotationsMatch(string input)
        {
            Match match = GetQuotationsMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetQuotationsMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _betweenQuotations.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static string GetDoubleQuotationsText(string input)
        {
            Match match = GetDoubleQuotationsMatch(input);
            return GetMatch(match);
        }
        public static string[] GetDoubleQuotationsTextAll(string input)
        {
            Match match = GetDoubleQuotationsMatch(input);
            return GetMatches(match);
        }
        public static bool IsDoubleQuotationsMatch(string input)
        {
            Match match = GetDoubleQuotationsMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetDoubleQuotationsMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _betweenQuotationsDouble.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static string GetSingleQuotationsText(string input)
        {
            Match match = GetSingleQuotationsMatch(input);
            return GetMatch(match);
        }
        public static string[] GetSingleQuotationsTextAll(string input)
        {
            Match match = GetSingleQuotationsMatch(input);
            return GetMatches(match);
        }
        public static bool IsSingleQuotationsMatch(string input)
        {
            Match match = GetSingleQuotationsMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetSingleQuotationsMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _betweenQuotationsSingle.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        #endregion

        #region Credit Cards
        public static bool IsCardSecurityCodeMatch(string input)
        {
            Match match = GetCardSecurityCodeMatch(input);
            return GetIsMatch(match);
        }
        public static string ReplaceCardSecurityCodeMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            MatchEvaluator matchEvaluator = new MatchEvaluator(CreditCardMatchEvaluator);
            try
            {
                return _cardSecurityCode.Replace(input, matchEvaluator);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        private static Match GetCardSecurityCodeMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _cardSecurityCode.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsCreditCardNumberMatch(string input)
        {
            Match match = GetCreditCardNumberMatch(input);
            return GetIsMatch(match);
        }
        public static string ReplaceCreditCardNumberMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            MatchEvaluator matchEvaluator = new MatchEvaluator(CreditCardMatchEvaluator);
            try
            {
                return _creditCardNumber.Replace(input, matchEvaluator);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        private static Match GetCreditCardNumberMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _creditCardNumber.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsCreditCardNumberAmericanExpressMatch(string input)
        {
            Match match = GetCreditCardNumberAmericanExpressMatch(input);
            return GetIsMatch(match);
        }
        public static string ReplaceCreditCardNumberAmericanExpressMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            MatchEvaluator matchEvaluator = new MatchEvaluator(CreditCardMatchEvaluator);
            try
            {
                return _creditCardNumberAmericanExpress.Replace(input, matchEvaluator);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        private static Match GetCreditCardNumberAmericanExpressMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _creditCardNumberAmericanExpress.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsCreditCardNumberDinersClubMatch(string input)
        {
            Match match = GetCreditCardNumberDinersClubMatch(input);
            return GetIsMatch(match);
        }
        public static string ReplaceCreditCardNumberDinersClubMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            MatchEvaluator matchEvaluator = new MatchEvaluator(CreditCardMatchEvaluator);
            try
            {
                return _creditCardNumberDinersClub.Replace(input, matchEvaluator);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        private static Match GetCreditCardNumberDinersClubMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _creditCardNumberDinersClub.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsCreditCardNumberDiscoverMatch(string input)
        {
            Match match = GetCreditCardNumberDiscoverMatch(input);
            return GetIsMatch(match);
        }
        public static string ReplaceCreditCardNumberDiscoverMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            MatchEvaluator matchEvaluator = new MatchEvaluator(CreditCardMatchEvaluator);
            try
            {
                return _creditCardNumberDiscover.Replace(input, matchEvaluator);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        private static Match GetCreditCardNumberDiscoverMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _creditCardNumberDiscover.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsCreditCardNumberMasterCardMatch(string input)
        {
            Match match = GetCreditCardNumberMasterCardMatch(input);
            return GetIsMatch(match);
        }
        public static string ReplaceCreditCardNumberMasterCardMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            MatchEvaluator matchEvaluator = new MatchEvaluator(CreditCardMatchEvaluator);
            try
            {
                return _creditCardNumberMasterCard.Replace(input, matchEvaluator);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        private static Match GetCreditCardNumberMasterCardMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _creditCardNumberMasterCard.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsCreditCardNumberJCBMatch(string input)
        {
            Match match = GetCreditCardNumberJCBMatch(input);
            return GetIsMatch(match);
        }
        public static string ReplaceCreditCardNumberJCBMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            MatchEvaluator matchEvaluator = new MatchEvaluator(CreditCardMatchEvaluator);
            try
            {
                return _creditCardNumberJCB.Replace(input, matchEvaluator);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        private static Match GetCreditCardNumberJCBMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _creditCardNumberJCB.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsCreditCardNumberVisaMatch(string input)
        {
            Match match = GetCreditCardNumberVisaMatch(input);
            return GetIsMatch(match);
        }
        public static string ReplaceCreditCardNumberVisaMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            MatchEvaluator matchEvaluator = new MatchEvaluator(CreditCardMatchEvaluator);
            try
            {
                return _creditCardNumberVisa.Replace(input, matchEvaluator);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        private static Match GetCreditCardNumberVisaMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _creditCardNumberVisa.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        private static string CreditCardMatchEvaluator(Match match)
        {
            int matchLength = match.Value.Length;
            return new string(_censorNumberCharacter, matchLength);
        }
        #endregion

        #region Email
        public static bool IsEmailMatch(string input)
        {
            Match match = GetEmailMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetEmailMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _email.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsEmailHtml5Match(string input)
        {
            Match match = GetEmailHtml5Match(input);
            return GetIsMatch(match);
        }
        private static Match GetEmailHtml5Match(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _emailHtml5.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsEmailRFC5322Match(string input)
        {
            Match match = GetEmailRFC5322Match(input);
            return GetIsMatch(match);
        }
        private static Match GetEmailRFC5322Match(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _emailRFC5322.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        #endregion

        #region Network and Internet
        public static bool IsHexColorMatch(string input)
        {
            Match match = GetHexColorMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetHexColorMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _hexColor.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsHtmlTagMatch(string input)
        {
            Match match = GetHtmlTagMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetHtmlTagMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _htmlTag.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsIpV4Match(string input)
        {
            Match match = GetIpV4Match(input);
            return GetIsMatch(match);
        }
        private static Match GetIpV4Match(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _ipv4.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsIpV6Match(string input)
        {
            Match match = GetIpV6Match(input);
            return GetIsMatch(match);
        }
        private static Match GetIpV6Match(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _ipv6.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsSlugMatch(string input)
        {
            Match match = GetSlugMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetSlugMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _slug.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsUsernameMatch(string input)
        {
            Match match = GetUsernameMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetUsernameMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _username.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        #endregion

        #region Personal Contact
        public static bool IsPhoneNumberMatch(string input)
        {
            Match match = GetPhoneNumberMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetPhoneNumberMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _phoneNumber.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsSocialSecurityNumberMatch(string input)
        {
            Match match = GetSocialSecurityNumberMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetSocialSecurityNumberMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _socialSecurityNumber.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsStatesUSMatch(string input)
        {
            Match match = GetStatesUSMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetStatesUSMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _statesUs.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static bool IsZipCodeUSMatch(string input)
        {
            Match match = GetZipCodeUSMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetZipCodeUSMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _zipCodeUs.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        #endregion

        #region Social Media
        public static string GetHashtag(string input)
        {
            Match match = GetHashtagMatch(input);
            return GetMatch(match);
        }
        public static string[] GetHashtags(string input)
        {
            Match match = GetHashtagMatch(input);
            return GetMatches(match);
        }
        public static bool IsHashtagMatch(string input)
        {
            Match match = GetHashtagMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetHashtagMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _hashtag.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static string GetHashtagStockTicker(string input)
        {
            Match match = GetHashtagStockTickerMatch(input);
            return GetMatch(match);
        }
        public static string[] GetHashtagStockTickers(string input)
        {
            Match match = GetHashtagStockTickerMatch(input);
            return GetMatches(match);
        }
        public static bool IsHashtagStockTickerMatch(string input)
        {
            Match match = GetHashtagStockTickerMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetHashtagStockTickerMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _hashtagStockTicker.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static string GetUsernameSocial(string input)
        {
            Match match = GetUsernameSocialMatch(input);
            return GetMatch(match);
        }
        public static string[] GetUsernamesSocial(string input)
        {
            Match match = GetUsernameSocialMatch(input);
            return GetMatches(match);
        }
        public static bool IsUsernameSocialMatch(string input)
        {
            Match match = GetUsernameSocialMatch(input);
            return GetIsMatch(match);
        }
        private static Match GetUsernameSocialMatch(string input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _usernameSocial.Match(input);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        #endregion

        #region Whitespace
        public static string CollapseWhitespace(string input)
        {
            MatchEvaluator matchEvaluator = new MatchEvaluator(CollapseWhitespaceMatchEvaluator);
            return CollapseWhitespace(input, matchEvaluator);
        }
        private static string CollapseWhitespaceMatchEvaluator(Match match)
        {
            return _space;
        }

        public static string CollapseWhitespaceToNewLine(string input)
        {
            MatchEvaluator matchEvaluator = new MatchEvaluator(CollapseWhitespaceToNewLineMatchEvaluator);
            return CollapseWhitespace(input, matchEvaluator);
        }
        private static string CollapseWhitespaceToNewLineMatchEvaluator(Match match)
        {
            return ConfigurationLayer.NewLine;
        }

        public static string CollapseWhitespaceToTab(string input)
        {
            MatchEvaluator matchEvaluator = new MatchEvaluator(CollapseWhitespaceToTabMatchEvaluator);
            return CollapseWhitespace(input, matchEvaluator);
        }
        private static string CollapseWhitespaceToTabMatchEvaluator(Match match)
        {
            return ConfigurationLayer.Tab;
        }

        private static string CollapseWhitespace(string input, MatchEvaluator matchEvaluator)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            try
            {
                return _whitespace.Replace(input, matchEvaluator);
            }
            catch (RegexMatchTimeoutException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        #endregion
    }
}
