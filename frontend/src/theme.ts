import { createTheme, type MantineThemeOverride } from '@mantine/core'

const githubDark = [
  '#e6edf3',
  '#c7c7c7',
  '#b0b0b0',
  '#9a9a9a',
  '#30363d',
  '#2a2a2a',
  '#161b22',
  '#0d1117',
  '#010409',
  '#010409',
] as const

export const theme: MantineThemeOverride = createTheme({
  primaryColor: 'accent',
  primaryShade: 6,
  colors: {
    accent: [
      '#eaf3ff',
      '#d1e5ff',
      '#b0d2ff',
      '#8cbcff',
      '#69a5fb',
      '#4a92f9',
      '#2f81f7',
      '#1f6feb',
      '#1a5fc7',
      '#164ea3',
    ],
    dark: [...githubDark],
  },
  black: '#010409',
  white: '#e6edf3',
  defaultRadius: 'md',
})
