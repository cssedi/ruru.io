/*
  Consent notice for sending plan details to the AI provider.

  These details include health information (special personal information under POPIA,
  section 26) and they leave South Africa, so the user must agree explicitly and be told
  the risks of the transfer (sections 11, 27 and 72(1)(b)).

  TODO before launch: have this wording checked by someone qualified, and confirm
  Anthropic's current data-handling terms. If the wording changes, bump AI_CONSENT_VERSION
  here and TrainingPlanService.CurrentAiConsentVersion in the API together.
*/

import { CONTACT_EMAIL } from '../onboarding/consent'

export const AI_CONSENT_VERSION = '2026-11-14'

export interface AiConsentPoint {
  heading: string
  body: string
}

export const AI_CONSENT_POINTS: AiConsentPoint[] = [
  {
    heading: 'What we send',
    body:
      'Your training answers (goal, experience, where and when you train, focus areas) and, where you’ve given them, ' +
      'your sex, age, height, weight, sleep and health considerations such as diabetes or sensitive knees. ' +
      'Never your name, email address or date of birth.',
  },
  {
    heading: 'Who gets it, and where',
    body:
      'Anthropic, the company behind the Claude AI model, which writes the plan. Its servers are in the United States, ' +
      'outside South Africa, where privacy laws differ from POPIA.',
  },
  {
    heading: 'Why',
    body: 'Only to write your training plan. ruru keeps the plan you save on its servers in South Africa.',
  },
  {
    heading: 'Your choice',
    body:
      'This is optional: you can keep logging workouts without it. You can stop at any time with “Stop using AI” ' +
      `under your plan, or by emailing ${CONTACT_EMAIL}.`,
  },
]
