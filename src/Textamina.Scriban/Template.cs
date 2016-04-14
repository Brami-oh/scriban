﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. See license.txt file in the project root for full license information.
using System;
using System.Collections.Generic;
using Textamina.Scriban.Parsing;
using Textamina.Scriban.Runtime;

namespace Textamina.Scriban
{
    /// <summary>
    /// Entry point class to parse templates and render them.
    /// </summary>
    public class Template
    {
        private Template(TemplateOptions options, string sourceFilePath)
        {
            Messages = new List<LogMessage>();
            Options = options ?? new TemplateOptions();
            this.SourceFilePath = sourceFilePath;
        }

        /// <summary>
        /// Gets the source file path.
        /// </summary>
        public string SourceFilePath { get; }

        /// <summary>
        /// Gets the current options of this template.
        /// </summary>
        public TemplateOptions Options { get; }

        /// <summary>
        /// Gets the resulting compiled <see cref="ScriptPage"/>. May be null if this template <see cref="HasErrors"/> 
        /// </summary>
        public ScriptPage Page { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this template has errors. Error messages are in <see cref="Messages"/>.
        /// </summary>
        public bool HasErrors { get; private set; }

        /// <summary>
        /// Gets the lexer and parsing messages.
        /// </summary>
        public List<LogMessage> Messages { get; private set; }

        /// <summary>
        /// Parses the specified scripting text into a <see cref="Template"/> .
        /// </summary>
        /// <param name="text">The scripting text.</param>
        /// <param name="sourceFilePath">The source file path. Optional, used for better error reporting if the source file has a location on the disk</param>
        /// <param name="options">The templating parsing options.</param>
        /// <returns>A template</returns>
        public static Template Parse(string text, string sourceFilePath = null, TemplateOptions options = null)
        {
            var template = new Template(options, sourceFilePath);
            template.ParseInternal(text, sourceFilePath);
            return template;
        }

        /// <summary>
        /// Renders this template using the specified context. See remarks.
        /// </summary>
        /// <param name="context">The template context.</param>
        /// <exception cref="System.ArgumentNullException">If context is null</exception>
        /// <exception cref="System.InvalidOperationException">If the template <see cref="HasErrors"/>. Check the <see cref="Messages"/> property for more details</exception>
        /// <remarks>
        /// When using this method, the result of rendering this page is output to <see cref="TemplateContext.Output"/>
        /// </remarks>
        public void Render(TemplateContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (HasErrors) throw new InvalidOperationException("This template has errors. Check the <Messages> property for more details");

            // Make sure that we are using the same options
            context.Options = Options;
            if (SourceFilePath != null)
            {
                context.PushSourceFile(SourceFilePath);
            }

            try
            {
                Page?.Evaluate(context);
            }
            finally
            {
                if (SourceFilePath != null)
                {
                    context.PopSourceFile();
                }
            }
        }

        /// <summary>
        /// Renders this template using the specified object model.
        /// </summary>
        /// <param name="model">The object model.</param>
        /// <returns>A rendering result as a string </returns>
        public string Render(object model = null)
        {
            var scriptObject = new ScriptObject();
            if (model != null)
            {
                scriptObject.Import(model);
            }

            var context = new TemplateContext(scriptObject, Options);
            Render(context);

            return context.Output.ToString();
        }

        private void ParseInternal(string text, string sourceFilePath)
        {
            // Early exit
            if (string.IsNullOrEmpty(text))
            {
                HasErrors = false;
                Messages = new List<LogMessage>();
                return;
            }

            var lexer = new Lexer(text, sourceFilePath, Options.Parser.Mode == ParsingMode.ScriptOnly);
            var parser = new Parser(lexer, Options.Parser);

            Page = parser.Run();

            HasErrors = parser.HasErrors;
            Messages = parser.Messages;
        }
    }
}