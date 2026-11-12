/*
  Consent notice for the health details asked in onboarding.

  Under POPIA, health information is special personal information (section 26), so
  processing it needs the user's explicit, specific and informed consent (sections 11
  and 27), and they must be told who is collecting it, why, and what their rights
  are (section 18).

  TODO before launch: replace every [placeholder] below and have the wording checked
  by someone qualified. If the wording changes, bump CONSENT_VERSION here and
  OnboardingService.CurrentConsentVersion in the API together, so every stored
  consent shows which text the user agreed to.
*/

export const CONSENT_VERSION = '2026-11-12'

export const RESPONSIBLE_PARTY = '[Company name]'
export const CONTACT_EMAIL = '[privacy@your-domain.co.za]'
export const PRIVACY_POLICY_URL = '[privacy policy URL]'
export const INFORMATION_REGULATOR_URL = 'https://inforegulator.org.za'

export interface ConsentPoint {
  heading: string
  body: string
}

export const CONSENT_POINTS: ConsentPoint[] = [
  {
    heading: 'What we’ll ask',
    body:
      'Your sex, date of birth, height and weight, health considerations such as diabetes or injuries, ' +
      'and your activity, sleep, training and eating habits.',
  },
  {
    heading: 'Why',
    body:
      'Only to personalise your workouts and nutrition targets in ruru. We don’t sell your details, ' +
      'use them for advertising or share them with anyone else.',
  },
  {
    heading: 'Where it’s kept',
    body:
      'On secure servers in South Africa (Amazon Web Services, Cape Town region), for as long as your ' +
      'account is open or until you withdraw your consent.',
  },
  {
    heading: 'Your choice',
    body:
      'Answering is voluntary. Choose “Not right now” and you can still use ruru with general targets. ' +
      `You can withdraw your consent at any time by emailing ${CONTACT_EMAIL}, and we’ll delete these details.`,
  },
  {
    heading: 'Your rights',
    body:
      'You can ask to see, correct or delete your information, or object to how we use it. ' +
      'If you’re unhappy with how we handle it, you can complain to the Information Regulator.',
  },
]
