import { Alert, Button, Group, Modal, Stack, TextInput } from '@mantine/core'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { updateUser, type User } from '../api/users'
import { errorMessage } from './errorMessage'

interface Props {
  user: User
  onClose: () => void
  onSaved: () => void
}

// Mounted only while open, so the fields start from the current user every time.
export function EditProfileModal({ user, onClose, onSaved }: Props) {
  const queryClient = useQueryClient()
  const [firstName, setFirstName] = useState(user.firstName)
  const [lastName, setLastName] = useState(user.lastName)

  const save = useMutation({
    mutationFn: () =>
      updateUser(user.id, { username: user.username, firstName: firstName.trim(), lastName: lastName.trim() }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['user', user.id] })
      onSaved()
      onClose()
    },
  })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    save.mutate()
  }

  return (
    <Modal opened onClose={onClose} title="Edit profile">
      <form onSubmit={submit}>
        <Stack>
          {save.error && (
            <Alert color="red" title="Could not save">
              {errorMessage(save.error)}
            </Alert>
          )}
          <TextInput label="First name" value={firstName} onChange={(e) => setFirstName(e.currentTarget.value)} data-autofocus />
          <TextInput label="Last name" value={lastName} onChange={(e) => setLastName(e.currentTarget.value)} />
          <TextInput label="Username" value={user.username} disabled readOnly />
          <TextInput label="Email" value={user.email} disabled readOnly />
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={save.isPending} disabled={firstName.trim() === '' || lastName.trim() === ''}>
              Save
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  )
}
