import { createTheme, type MantineThemeOverride } from '@mantine/core'

export const theme: MantineThemeOverride = createTheme({
  primaryColor: 'graphite',
  primaryShade: 6,
  colors: {
    graphite: [
      '#e4e6ea',
      '#c9ccd2',
      '#a9adb6',
      '#8a8f9b',
      '#6c7280',
      '#515664',
      '#3d424e',
      '#2e323c',
      '#23262e',
      '#181a20',
    ],
    dark: [
      '#d5d8de',
      '#b8bcc5',
      '#9a9fab',
      '#7d8290',
      '#5f6472',
      '#454a57',
      '#343842',
      '#2a2d36',
      '#1f2229',
      '#15171c',
    ],
  },
  black: '#15171c',
  white: '#e4e6ea',
  defaultRadius: 'md',
})
