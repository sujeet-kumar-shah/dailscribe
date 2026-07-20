---
title: "Mastering Daily Digests: How to Get the Most Out of DayScribe"
excerpt: "Learn how to configure your AI prompts, use tags effectively, and turn your daily activity logs into actionable learning insights."
author: "DayScribe Team"
date: 2026-07-18
tags: ["productivity", "AI", "learning"]
cover: ""
---

## Why Daily Digests Matter

We consume enormous amounts of information every day — articles, code, meetings, discussions. Without reflection, most of it fades away. DayScribe's daily digest bridges the gap between activity and learning.

## Configuring Your AI Provider

DayScribe supports two AI backends:

### Ollama (Recommended)

1. Install [Ollama](https://ollama.ai)
2. Pull a model: `ollama pull llama3.2:1b`
3. DayScribe uses it automatically

### OpenAI

1. Get an API key from [OpenAI](https://platform.openai.com)
2. Add it to `appsettings.json` under `AppConfig:OpenAI:ApiKey`
3. Digests will use GPT-4o-mini

## Understanding Your Digest

A typical daily digest includes:

- **Top Applications** — Where you spent most of your time
- **Browsed Domains** — Websites and articles you visited
- **Article Summaries** — Key takeaways from long-form content
- **Learning Highlights** — AI-generated insights based on your activity

## Improving Digest Quality

- Name your windows descriptively
- Use the browser extension consistently
- Review and rate your digests to help the AI improve

## Privacy First

Remember: with Ollama, everything runs locally. No data ever leaves your machine.
