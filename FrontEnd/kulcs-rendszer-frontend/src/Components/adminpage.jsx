import { useEffect, useState } from "react";
import { getUserFromToken, jsonOptions, request } from "../api.js";
import ErrorToast from "./errorToast.jsx";

function getFloorDisplayName(floor) {
  if (!Number.isInteger(floor?.number)) return "Ismeretlen emelet";
  return floor.number === 0 ? "Földszint" : `${floor.number}. emelet`;
}

function getRoleName(name) {
  const roleNames = ["Oktató", "Portás", "Admin", "Igazgató"];
  if (typeof name === "number") return roleNames[name] ?? `Szerepkör ${name}`;
  if (typeof name === "string") {
    if (isNoRoleName(name)) return "Nincs szerepkör";
    const roleIndex = Number(name);
    if (Number.isInteger(roleIndex) && roleNames[roleIndex]) {
      return roleNames[roleIndex];
    }
    return name === "Oktato" ? "Oktató" : name;
  }
  return "Ismeretlen szerepkör";
}

function isNoRoleName(name) {
  if (typeof name !== "string") return false;
  const normalized = name
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLocaleLowerCase("hu")
    .replace(/[\s_-]/g, "");

  return [
    "norole",
    "none",
    "unassigned",
    "nincsszerep",
    "nincsszerepkor",
  ].includes(normalized);
}

function formatBookingDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString("hu-HU");
}

export default function AdminPage() {
  const [section, setSection] = useState("master-data");
  const [buildings, setBuildings] = useState([]);
  const [floors, setFloors] = useState([]);
  const [rooms, setRooms] = useState([]);
  const [buildingSearch, setBuildingSearch] = useState("");
  const [floorSearch, setFloorSearch] = useState("");
  const [roomSearch, setRoomSearch] = useState("");
  const [roomSearchResults, setRoomSearchResults] = useState(null);
  const [roomSearchLoading, setRoomSearchLoading] = useState(false);
  const [roomSearchError, setRoomSearchError] = useState("");
  const [selectedBuildingId, setSelectedBuildingId] = useState(null);
  const [selectedFloorId, setSelectedFloorId] = useState(null);
  const [selectedRoomKey, setSelectedRoomKey] = useState(null);
  const [buildingForm, setBuildingForm] = useState(null);
  const [floorForm, setFloorForm] = useState(null);
  const [roomForm, setRoomForm] = useState(null);
  const [loading, setLoading] = useState(true);
  const [roomsLoading, setRoomsLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [roomError, setRoomError] = useState("");
  const [notice, setNotice] = useState("");
  const [userIdInput, setUserIdInput] = useState("");
  const [users, setUsers] = useState([]);
  const [roles, setRoles] = useState([]);
  const [userSearch, setUserSearch] = useState("");
  const [usersLoading, setUsersLoading] = useState(false);
  const [rolesLoading, setRolesLoading] = useState(false);
  const [rolesError, setRolesError] = useState("");
  const [selectedUser, setSelectedUser] = useState(null);
  const [userForm, setUserForm] = useState(null);
  const [roleIdInput, setRoleIdInput] = useState("");
  const [bookingRooms, setBookingRooms] = useState([]);
  const [bookingSearch, setBookingSearch] = useState("");
  const [adminBookings, setAdminBookings] = useState([]);
  const [bookingDirectoryLoading, setBookingDirectoryLoading] = useState(false);
  const [adminBookingsLoading, setAdminBookingsLoading] = useState(false);
  const [adminBookingsError, setAdminBookingsError] = useState("");
  const [bookingRoomError, setBookingRoomError] = useState("");
  const [adminBookingsNotice, setAdminBookingsNotice] = useState("");
  const [bookingRefreshKey, setBookingRefreshKey] = useState(0);
  const [deletingBookingId, setDeletingBookingId] = useState(null);

  const refreshStructure = async () => {
    const [buildingData, floorData] = await Promise.all([
      request("/Building/getall"),
      request("/Floor/getall"),
    ]);

    const nextBuildingId = buildingData.some(
      (building) => building.id === selectedBuildingId,
    )
      ? selectedBuildingId
      : (buildingData[0]?.id ?? null);
    const availableFloors = floorData.filter(
      (floor) => floor.buildingId === nextBuildingId,
    );

    setBuildings(buildingData);
    setFloors(floorData);
    setSelectedBuildingId(nextBuildingId);
    setSelectedFloorId((currentFloorId) =>
      availableFloors.some((floor) => floor.id === currentFloorId)
        ? currentFloorId
        : (availableFloors[0]?.id ?? null),
    );
  };

  const refreshRooms = async () => {
    setRoomError("");
    try {
      const roomData = await request("/Room/getall");
      setRooms(roomData);
    } catch (loadError) {
      setRooms([]);
      setRoomError(`Nem sikerült betölteni a termeket: ${loadError.message}`);
    } finally {
      setRoomsLoading(false);
    }
  };

  useEffect(() => {
    if (section !== "users") return undefined;

    let active = true;
    const loadUsersAndRoles = async () => {
      setUsersLoading(true);
      setRolesLoading(true);
      setError("");
      setRolesError("");
      const [usersResult, rolesResult] = await Promise.allSettled([
        request("/User"),
        request("/User/roles"),
      ]);

      if (active) {
        if (
          usersResult.status === "fulfilled" &&
          Array.isArray(usersResult.value)
        ) {
          setUsers(usersResult.value);
        } else {
          const message =
            usersResult.status === "rejected"
              ? usersResult.reason.message
              : "A felhasználólista formátuma érvénytelen.";
          setError(`Nem sikerült betölteni a felhasználókat: ${message}`);
        }

        if (
          rolesResult.status === "fulfilled" &&
          Array.isArray(rolesResult.value)
        ) {
          setRoles(rolesResult.value);
        } else {
          const message =
            rolesResult.status === "rejected"
              ? rolesResult.reason.message
              : "A szerepkörlista formátuma érvénytelen.";
          setRolesError(`Nem sikerült betölteni a szerepköröket: ${message}`);
        }

        setUsersLoading(false);
        setRolesLoading(false);
      }
    };

    loadUsersAndRoles();
    return () => {
      active = false;
    };
  }, [section]);

  useEffect(() => {
    if (section !== "bookings") return undefined;

    let active = true;
    const loadBookingData = async () => {
      setBookingDirectoryLoading(true);
      setAdminBookingsError("");
      setBookingRoomError("");
      const roomsResult = await Promise.allSettled([request("/Room/getall")]);

      if (active) {
        const roomResult = roomsResult[0];
        if (
          roomResult.status === "fulfilled" &&
          Array.isArray(roomResult.value)
        ) {
          setBookingRooms(roomResult.value);
        } else {
          const message =
            roomResult.status === "rejected"
              ? roomResult.reason.message
              : "A teremlista formátuma érvénytelen.";
          setBookingRooms([]);
          setBookingRoomError(`Nem sikerült betölteni a termeket: ${message}`);
        }

        setBookingDirectoryLoading(false);
      }
    };

    loadBookingData();
    return () => {
      active = false;
    };
  }, [bookingRefreshKey, section]);

  useEffect(() => {
    if (section !== "bookings") return undefined;

    let active = true;
    const loadAllBookings = async () => {
      setAdminBookingsLoading(true);
      setAdminBookingsError("");
      try {
        const result = await request("/bookings/getall");
        if (!Array.isArray(result)) {
          throw new Error("A foglalások API-válasza érvénytelen formátumú.");
        }
        if (active) {
          setAdminBookings(
            [...result].sort(
              (first, second) =>
                new Date(first.startTime).getTime() -
                new Date(second.startTime).getTime(),
            ),
          );
        }
      } catch (loadError) {
        if (active) {
          setAdminBookings([]);
          setAdminBookingsError(
            `Nem sikerült betölteni a foglalásokat: ${loadError.message}`,
          );
        }
      } finally {
        if (active) setAdminBookingsLoading(false);
      }
    };

    loadAllBookings();
    return () => {
      active = false;
    };
  }, [bookingRefreshKey, section]);

  useEffect(() => {
    let active = true;

    const loadStructure = async () => {
      try {
        const [buildingData, floorData] = await Promise.all([
          request("/Building/getall"),
          request("/Floor/getall"),
        ]);
        if (!active) return;

        const firstBuildingId = buildingData[0]?.id ?? null;
        const firstFloor = floorData.find(
          (floor) => floor.buildingId === firstBuildingId,
        );
        setBuildings(buildingData);
        setFloors(floorData);
        setSelectedBuildingId(firstBuildingId);
        setSelectedFloorId(firstFloor?.id ?? null);
      } catch (loadError) {
        if (active) {
          setError(
            `Nem sikerült betölteni az épületeket és emeleteket: ${loadError.message}`,
          );
        }
      } finally {
        if (active) setLoading(false);
      }
    };

    loadStructure();
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    let active = true;
    const loadRooms = async () => {
      try {
        const roomData = await request("/Room/getall");
        if (active) {
          setRooms(roomData);
          setRoomError("");
        }
      } catch (loadError) {
        if (active) {
          setRooms([]);
          setRoomError(
            `Nem sikerült betölteni a termeket: ${loadError.message}`,
          );
        }
      } finally {
        if (active) setRoomsLoading(false);
      }
    };

    loadRooms();
    return () => {
      active = false;
    };
  }, []);

  const runMutation = async (operation, successMessage) => {
    setSaving(true);
    setError("");
    setNotice("");
    try {
      await operation();
      await Promise.all([refreshStructure(), refreshRooms()]);
      setNotice(successMessage);
      return true;
    } catch (mutationError) {
      setError(`A művelet nem sikerült: ${mutationError.message}`);
      return false;
    } finally {
      setSaving(false);
    }
  };

  const selectedBuilding = buildings.find(
    (building) => building.id === selectedBuildingId,
  );
  const selectedFloor = floors.find((floor) => floor.id === selectedFloorId);
  const matchingUsers = users.filter((user) => {
    const query = userSearch.trim().toLocaleLowerCase("hu");
    return (
      !query ||
      String(user.id).includes(query) ||
      user.name.toLocaleLowerCase("hu").includes(query) ||
      user.email.toLocaleLowerCase("hu").includes(query) ||
      (user.roleId != null &&
        (String(user.roleId).includes(query) ||
          getRoleName(roles.find((role) => role.id === user.roleId)?.name)
            .toLocaleLowerCase("hu")
            .includes(query)))
    );
  });
  const selectedRole = roles.find((role) => role.id === selectedUser?.roleId);
  const roomById = Object.fromEntries(
    bookingRooms.map((room) => [room.id, room]),
  );
  const normalizedBookingSearch = bookingSearch.trim().toLocaleLowerCase("hu");
  const matchingAdminBookings = adminBookings.filter((booking) => {
    const room = roomById[booking.roomId];
    return (
      !normalizedBookingSearch ||
      booking.roomId
        .toLocaleLowerCase("hu")
        .includes(normalizedBookingSearch) ||
      room?.name.toLocaleLowerCase("hu").includes(normalizedBookingSearch) ||
      room?.building.toLocaleLowerCase("hu").includes(normalizedBookingSearch)
    );
  });
  const selectedUserHasNoRole =
    selectedUser?.roleId == null || isNoRoleName(selectedRole?.name);
  const buildingFloors = floors.filter(
    (floor) => floor.buildingId === selectedBuildingId,
  );
  const normalizedFloorSearch = floorSearch.trim().toLocaleLowerCase("hu");
  const matchingFloors = buildingFloors.filter(
    (floor) =>
      !normalizedFloorSearch ||
      String(floor.number)
        .toLocaleLowerCase("hu")
        .includes(normalizedFloorSearch) ||
      getFloorDisplayName(floor)
        .toLocaleLowerCase("hu")
        .includes(normalizedFloorSearch),
  );
  const matchingBuildings = buildings.filter((building) => {
    const query = buildingSearch.trim().toLocaleLowerCase("hu");
    return (
      !query ||
      building.buildingName.toLocaleLowerCase("hu").includes(query) ||
      building.buildingCode.toLocaleLowerCase("hu").includes(query)
    );
  });
  const visibleRooms =
    selectedFloorId === null
      ? []
      : rooms.filter((room) => room.floorId === selectedFloorId);
  const roomQuery = roomSearch.trim().toLocaleLowerCase("hu");
  const matchingRooms = visibleRooms.filter(
    (room) =>
      !roomQuery ||
      room.name.toLocaleLowerCase("hu").includes(roomQuery) ||
      room.id.toLocaleLowerCase("hu").includes(roomQuery) ||
      room.equipment.toLocaleLowerCase("hu").includes(roomQuery) ||
      room.building.toLocaleLowerCase("hu").includes(roomQuery),
  );
  const visibleRoomError = roomError;
  const roomIsLoading = roomsLoading;

  const saveBuilding = async (event) => {
    event.preventDefault();
    const buildingId = buildingForm.id;
    const payload = {
      buildingCode: buildingForm.buildingCode.trim(),
      buildingName: buildingForm.buildingName.trim(),
    };
    if (!payload.buildingCode || !payload.buildingName) {
      setError("Az épületkód és az épület neve nem lehet üres.");
      return;
    }

    const isEditing = buildingId !== undefined;

    const saved = await runMutation(
      () =>
        request(
          isEditing ? `/Building/update/${buildingId}` : "/Building/create",
          jsonOptions(isEditing ? "PUT" : "POST", payload),
        ),
      isEditing ? "Az épület adatai frissültek." : "Az épület létrejött.",
    );
    if (saved) setBuildingForm(null);
  };

  const deleteBuilding = async (building) => {
    if (
      !window.confirm(`Biztosan törlöd a(z) ${building.buildingCode} épületet?`)
    ) {
      return;
    }

    const deleted = await runMutation(
      () => request(`/Building/delete/${building.id}`, { method: "DELETE" }),
      "Az épület törlődött.",
    );
    if (deleted) {
      setBuildingForm(null);
      setFloorForm(null);
    }
  };

  const saveFloor = async (event) => {
    event.preventDefault();
    const floorId = floorForm.id;
    const payload = {
      number: Number(floorForm.number),
      buildingId: selectedBuildingId,
    };
    const isEditing = floorId !== undefined;

    const saved = await runMutation(
      () =>
        request(
          isEditing ? `/Floor/update/${floorId}` : "/Floor/create",
          jsonOptions(isEditing ? "PUT" : "POST", payload),
        ),
      isEditing ? "Az emelet adatai frissültek." : "Az emelet létrejött.",
    );
    if (saved) setFloorForm(null);
  };

  const deleteFloor = async (floor) => {
    if (
      !window.confirm(
        `Biztosan törlöd a kijelölt emeletet (${getFloorDisplayName(floor)})?`,
      )
    ) {
      return;
    }

    const deleted = await runMutation(
      () => request(`/Floor/delete/${floor.id}`, { method: "DELETE" }),
      "Az emelet törlődött.",
    );
    if (deleted) setFloorForm(null);
  };

  const saveRoom = async (event) => {
    event.preventDefault();
    const isEditing = roomForm.isEditing;
    const payload = {
      floorId: Number(roomForm.floorId),
      name: roomForm.name.trim(),
      capacity: Number(roomForm.capacity),
      equipment: roomForm.equipment.trim(),
    };

    if (
      (!isEditing && !roomForm.id.trim()) ||
      !Number.isInteger(payload.floorId) ||
      payload.floorId < 1 ||
      !payload.name ||
      !Number.isInteger(payload.capacity) ||
      payload.capacity < 0
    ) {
      setError(
        "Adj meg érvényes teremazonosítót, emeletet, nevet és férőhelyet.",
      );
      return;
    }

    const saved = await runMutation(
      () =>
        request(
          isEditing
            ? `/Room/update/${encodeURIComponent(roomForm.id)}`
            : "/Room/create",
          jsonOptions(isEditing ? "PUT" : "POST", {
            ...(isEditing ? {} : { id: roomForm.id.trim() }),
            ...payload,
          }),
        ),
      isEditing ? "A terem adatai frissültek." : "A terem létrejött.",
    );
    if (saved) {
      setRoomForm(null);
      setRoomSearchResults(null);
      const targetFloor = floors.find((floor) => floor.id === payload.floorId);
      if (targetFloor) setSelectedBuildingId(targetFloor.buildingId);
      setSelectedFloorId(payload.floorId);
      setSelectedRoomKey(
        `${payload.floorId}-${isEditing ? roomForm.id : roomForm.id.trim()}`,
      );
    }
  };

  const editRoom = (room) => {
    setSelectedFloorId(room.floorId);
    const roomFloor = floors.find((floor) => floor.id === room.floorId);
    if (roomFloor) setSelectedBuildingId(roomFloor.buildingId);
    setSelectedRoomKey(`${room.floorId}-${room.id}`);
    setRoomForm({
      id: room.id,
      isEditing: true,
      floorId: String(room.floorId),
      name: room.name,
      capacity: String(room.capacity),
      equipment: room.equipment,
    });
    setError("");
    setNotice("");
  };

  const deleteRoom = async (room) => {
    if (!window.confirm(`Biztosan törlöd a(z) ${room.name} termet?`)) {
      return;
    }

    const deleted = await runMutation(
      () =>
        request(`/Room/delete/${encodeURIComponent(room.id)}`, {
          method: "DELETE",
        }),
      "A terem törlődött.",
    );
    if (deleted) {
      setRoomForm(null);
      setRoomSearchResults(null);
      setSelectedRoomKey(null);
    }
  };

  const deleteAdminBooking = async (booking) => {
    if (
      !window.confirm(
        `Biztosan törlöd a(z) ${booking.userId}. felhasználó foglalását ` +
          `(${booking.id})?`,
      )
    ) {
      return;
    }

    setDeletingBookingId(booking.id);
    setAdminBookingsError("");
    setAdminBookingsNotice("");
    try {
      await request(`/bookings?bookingId=${encodeURIComponent(booking.id)}`, {
        method: "DELETE",
      });
      setAdminBookings((currentBookings) =>
        currentBookings.filter((item) => item.id !== booking.id),
      );
      setAdminBookingsNotice("A foglalás törölve.");
    } catch (deleteError) {
      setAdminBookingsError(
        `A foglalás törlése nem sikerült: ${deleteError.message}`,
      );
    } finally {
      setDeletingBookingId(null);
    }
  };

  const searchRooms = async (event) => {
    event.preventDefault();
    const query = roomSearch.trim().toLocaleLowerCase("hu");
    if (!query) {
      setRoomSearchResults(null);
      setRoomSearchError("");
      return;
    }

    setRoomSearchLoading(true);
    setRoomSearchError("");
    try {
      const allRooms = await request("/Room/getall");
      const results = allRooms.filter(
        (room) =>
          room.name.toLocaleLowerCase("hu").includes(query) ||
          room.id.toLocaleLowerCase("hu").includes(query) ||
          room.building.toLocaleLowerCase("hu").includes(query) ||
          room.equipment.toLocaleLowerCase("hu").includes(query),
      );
      setRooms(allRooms);
      setRoomSearchResults(
        results.map((room) => ({
          ...room,
          floorNumber: getFloorDisplayName(
            floors.find((floor) => floor.id === room.floorId),
          ),
        })),
      );
    } catch (searchError) {
      setRoomSearchResults(null);
      setRoomSearchError(`A teremkeresés nem sikerült: ${searchError.message}`);
    } finally {
      setRoomSearchLoading(false);
    }
  };

  const loadUser = async (userId) => {
    setSaving(true);
    setError("");
    setNotice("");
    try {
      const user = await request(`/User/info/${userId}`);
      setSelectedUser(user);
      setRoleIdInput(
        user.roleId == null ||
          isNoRoleName(roles.find((role) => role.id === user.roleId)?.name)
          ? ""
          : String(user.roleId),
      );
      setUsers((currentUsers) => {
        const exists = currentUsers.some(
          (currentUser) => currentUser.id === user.id,
        );
        return exists
          ? currentUsers.map((currentUser) =>
              currentUser.id === user.id ? user : currentUser,
            )
          : [user, ...currentUsers];
      });
      setUserIdInput(String(user.id));
      setRoleIdInput(
        user.roleId == null ||
          isNoRoleName(roles.find((role) => role.id === user.roleId)?.name)
          ? ""
          : String(user.roleId),
      );
      setUserForm(null);
    } catch (loadError) {
      setSelectedUser(null);
      setUserForm(null);
      setError(`A felhasználó betöltése nem sikerült: ${loadError.message}`);
    } finally {
      setSaving(false);
    }
  };

  const submitUserForm = async (event) => {
    event.preventDefault();
    const payload = {
      name: userForm.name.trim(),
      email: userForm.email.trim(),
      password: userForm.password,
    };

    if (!payload.name || !payload.email || !payload.password) {
      setError("A név, e-mail-cím és jelszó megadása kötelező.");
      return;
    }

    setSaving(true);
    setError("");
    setNotice("");
    try {
      const token = await request("/User/create", jsonOptions("POST", payload));
      if (typeof token !== "string" || !token) {
        throw new Error(
          "A felhasználó létrejött, de nem érkezett azonosító token.",
        );
      }
      const createdUserClaims = getUserFromToken(token);
      const createdUserId = Number(createdUserClaims.id);
      if (!Number.isInteger(createdUserId) || createdUserId < 1) {
        throw new Error(
          "A létrehozási token nem tartalmaz felhasználói azonosítót.",
        );
      }
      const user = {
        id: createdUserId,
        roleId: null,
        name: createdUserClaims.name || payload.name,
        email: createdUserClaims.email || payload.email,
      };
      setSelectedUser(user);
      setUsers((currentUsers) => {
        const existingIndex = currentUsers.findIndex(
          (currentUser) => currentUser.id === user.id,
        );
        if (existingIndex < 0) return [user, ...currentUsers];
        return currentUsers.map((currentUser) =>
          currentUser.id === user.id ? user : currentUser,
        );
      });
      setUserIdInput(String(user.id));
      setRoleIdInput(user.roleId == null ? "" : String(user.roleId));
      setUserForm(null);
      setNotice(
        `A felhasználó létrejött. Azonosítója: ${user.id}. Ha szükséges, rendelj hozzá szerepkört.`,
      );
    } catch (saveError) {
      setError(`A felhasználó mentése nem sikerült: ${saveError.message}`);
    } finally {
      setSaving(false);
    }
  };

  const deleteUser = async () => {
    if (
      !selectedUser ||
      !window.confirm(
        `Biztosan törlöd ezt a felhasználót: ${selectedUser.name}?`,
      )
    ) {
      return;
    }

    setSaving(true);
    setError("");
    setNotice("");
    try {
      await request(`/User/${selectedUser.id}`, { method: "DELETE" });
      setNotice(`A(z) ${selectedUser.id} azonosítójú felhasználó törlődött.`);
      setUsers((currentUsers) =>
        currentUsers.filter((user) => user.id !== selectedUser.id),
      );
      setSelectedUser(null);
      setUserForm(null);
      setRoleIdInput("");
      setUserIdInput("");
    } catch (deleteError) {
      setError(`A felhasználó törlése nem sikerült: ${deleteError.message}`);
    } finally {
      setSaving(false);
    }
  };

  const assignRole = async (event) => {
    event.preventDefault();
    const roleId = Number(roleIdInput);
    const isRoleChange = selectedUser?.roleId != null;
    if (
      !selectedUser ||
      !Number.isInteger(roleId) ||
      !roles.some((role) => role.id === roleId)
    ) {
      setError("Válassz egy érvényes szerepkört a listából.");
      return;
    }
    if (isNoRoleName(roles.find((role) => role.id === roleId)?.name)) {
      setError("A no-role szerepkör nem rendelhető hozzá cél szerepkörként.");
      return;
    }

    setSaving(true);
    setError("");
    setNotice("");
    try {
      await request(
        `/User/role/${selectedUser.id}`,
        jsonOptions("POST", roleId),
      );
      const updatedUser = await request(`/User/info/${selectedUser.id}`);
      if (updatedUser.roleId !== roleId) {
        throw new Error(
          "A kérés lefutott, de a friss felhasználói adatok nem igazolják a szerepkör hozzárendelését.",
        );
      }
      setSelectedUser(updatedUser);
      setUsers((currentUsers) =>
        currentUsers.map((user) =>
          user.id === updatedUser.id ? { ...user, ...updatedUser } : user,
        ),
      );
      setRolesError("");
      setRoleIdInput(String(updatedUser.roleId));
      setNotice(
        isRoleChange
          ? "A szerepkör módosítása sikerült."
          : "A szerepkör hozzárendelése sikerült.",
      );
    } catch (assignError) {
      setError(
        `A szerepkör hozzárendelése nem sikerült: ${assignError.message}`,
      );
    } finally {
      setSaving(false);
    }
  };

  const removeRole = async () => {
    if (
      !selectedUser ||
      selectedUser.roleId == null ||
      !window.confirm(
        `Biztosan törlöd ${selectedUser.name} jelenlegi szerepkörét?`,
      )
    ) {
      return;
    }

    setSaving(true);
    setError("");
    setNotice("");
    try {
      await request(`/User/role/remove/${selectedUser.id}`, {
        method: "DELETE",
      });
      const updatedUser = await request(`/User/info/${selectedUser.id}`);
      if (updatedUser.roleId != null) {
        throw new Error(
          "A kérés lefutott, de a felhasználó szerepköre nem lett törölve.",
        );
      }

      setSelectedUser(updatedUser);
      setUsers((currentUsers) =>
        currentUsers.map((user) =>
          user.id === updatedUser.id ? { ...user, ...updatedUser } : user,
        ),
      );
      setRoleIdInput("");
      setNotice("A felhasználó szerepköre törlődött.");
    } catch (removeError) {
      setError(`A szerepkör törlése nem sikerült: ${removeError.message}`);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <main className="admin-page">
        <p className="admin-loading">Adminisztrációs adatok betöltése...</p>
      </main>
    );
  }

  return (
    <main className="admin-page">
      <header className="admin-heading">
        <div>
          <h1>Adminisztráció</h1>
        </div>
      </header>

      <nav className="admin-tabs" aria-label="Adminisztrációs területek">
        <button
          type="button"
          className={section === "master-data" ? "active" : ""}
          onClick={() => setSection("master-data")}
        >
          Helyszínek és termek
        </button>
        <button
          type="button"
          className={section === "bookings" ? "active" : ""}
          onClick={() => setSection("bookings")}
        >
          Foglalások
        </button>
        <button
          type="button"
          className={section === "permissions" ? "active" : ""}
          onClick={() => setSection("permissions")}
        >
          Mesterkulcs-jogosultságok
        </button>
        <button
          type="button"
          className={section === "maintenance" ? "active" : ""}
          onClick={() => setSection("maintenance")}
        >
          Karbantartás
        </button>
        <button
          type="button"
          className={section === "users" ? "active" : ""}
          onClick={() => setSection("users")}
        >
          Felhasználók
        </button>
      </nav>

      <ErrorToast message={error} onDismiss={() => setError("")} />
      {notice && (
        <div className="admin-alert admin-alert-success" role="status">
          {notice}
        </div>
      )}

      {section === "master-data" && (
        <div className="admin-content">
          <section className="admin-panel">
            <div className="admin-panel-heading">
              <div>
                <p className="admin-eyebrow">ALAPADATOK</p>
                <h2>Épületek</h2>
              </div>
              <button
                type="button"
                className="admin-button admin-button-primary"
                onClick={() =>
                  setBuildingForm({ buildingCode: "", buildingName: "" })
                }
                disabled={saving}
              >
                Új épület
              </button>
            </div>

            {buildingForm && (
              <form className="admin-form" onSubmit={saveBuilding}>
                <h3>
                  {buildingForm.id === undefined
                    ? "Épület hozzáadása"
                    : "Épület szerkesztése"}
                </h3>
                <label>
                  Épületkód
                  <input
                    required
                    maxLength={30}
                    value={buildingForm.buildingCode}
                    onChange={(event) =>
                      setBuildingForm({
                        ...buildingForm,
                        buildingCode: event.target.value,
                      })
                    }
                    placeholder="Pl. A"
                  />
                </label>
                <label>
                  Épület neve
                  <input
                    required
                    maxLength={100}
                    value={buildingForm.buildingName}
                    onChange={(event) =>
                      setBuildingForm({
                        ...buildingForm,
                        buildingName: event.target.value,
                      })
                    }
                    placeholder="Pl. Főépület"
                  />
                </label>
                <div className="admin-form-actions">
                  <button
                    type="button"
                    className="admin-button admin-button-quiet"
                    onClick={() => setBuildingForm(null)}
                    disabled={saving}
                  >
                    Mégse
                  </button>
                  <button
                    type="submit"
                    className="admin-button admin-button-primary"
                    disabled={saving}
                  >
                    {saving ? "Mentés..." : "Mentés"}
                  </button>
                </div>
              </form>
            )}

            <label className="admin-search-field">
              <span>Épület keresése név vagy kód alapján</span>
              <input
                type="search"
                value={buildingSearch}
                onChange={(event) => setBuildingSearch(event.target.value)}
                placeholder="Pl. Főépület vagy A"
              />
            </label>
            {buildingSearch.trim() && (
              <button
                type="button"
                className="admin-text-button"
                onClick={() => setBuildingSearch("")}
              >
                Épületszűrő törlése
              </button>
            )}

            {buildingSearch.trim() && (
              <p className="admin-count" role="status">
                {matchingBuildings.length} épület található
              </p>
            )}

            <div className="admin-record-list">
              {buildings.length === 0 ? (
                <p className="admin-empty">Még nincs rögzített épület.</p>
              ) : matchingBuildings.length === 0 ? (
                <p className="admin-empty">
                  Nincs a keresésnek megfelelő épület.
                </p>
              ) : (
                matchingBuildings.map((building) => (
                  <article
                    className={`admin-record ${
                      selectedBuildingId === building.id ? "selected" : ""
                    }`}
                    key={building.id}
                  >
                    <button
                      type="button"
                      className="admin-record-select"
                      aria-pressed={selectedBuildingId === building.id}
                      onClick={() => {
                        setSelectedBuildingId(building.id);
                        setFloorSearch("");
                        setSelectedFloorId(
                          floors.find(
                            (floor) => floor.buildingId === building.id,
                          )?.id ?? null,
                        );
                        setSelectedRoomKey(null);
                        setFloorForm(null);
                      }}
                    >
                      <span className="admin-record-icon" aria-hidden="true">
                        {building.buildingCode.slice(0, 2)}
                      </span>
                      <span>
                        <strong>{building.buildingName}</strong>
                        <small>{building.buildingCode}</small>
                      </span>
                    </button>
                    <div className="admin-record-actions">
                      <button
                        type="button"
                        onClick={() => setBuildingForm({ ...building })}
                        aria-label={`Szerkesztés: ${building.buildingName}`}
                      >
                        Szerkesztés
                      </button>
                      <button
                        type="button"
                        className="admin-danger-link"
                        onClick={() => deleteBuilding(building)}
                        disabled={saving}
                        aria-label={`Törlés: ${building.buildingName}`}
                      >
                        Törlés
                      </button>
                    </div>
                  </article>
                ))
              )}
            </div>
          </section>

          <section className="admin-panel admin-location-panel">
            <div className="admin-panel-heading">
              <div>
                <p className="admin-eyebrow">KIVÁLASZTOTT ÉPÜLET</p>
                <h2>{selectedBuilding?.buildingName ?? "Nincs épület"}</h2>
              </div>
              {selectedBuilding && (
                <button
                  type="button"
                  className="admin-button admin-button-primary"
                  onClick={() =>
                    setFloorForm({ number: "", buildingId: selectedBuildingId })
                  }
                  disabled={saving}
                >
                  Új emelet
                </button>
              )}
            </div>

            {selectedBuilding && (
              <>
                <p className="admin-building-code">
                  Épületkód: <strong>{selectedBuilding.buildingCode}</strong>
                </p>
                {floorForm && (
                  <form
                    className="admin-form admin-form-inline"
                    onSubmit={saveFloor}
                  >
                    <h3>
                      {floorForm.id === undefined
                        ? "Emelet hozzáadása"
                        : "Emelet szerkesztése"}
                    </h3>
                    <label>
                      Emelet száma
                      <input
                        required
                        type="number"
                        step="1"
                        value={floorForm.number}
                        onChange={(event) =>
                          setFloorForm({
                            ...floorForm,
                            number: event.target.value,
                          })
                        }
                        placeholder="Pl. 1"
                      />
                    </label>
                    <div className="admin-form-actions">
                      <button
                        type="button"
                        className="admin-button admin-button-quiet"
                        onClick={() => setFloorForm(null)}
                        disabled={saving}
                      >
                        Mégse
                      </button>
                      <button
                        type="submit"
                        className="admin-button admin-button-primary"
                        disabled={saving}
                      >
                        {saving ? "Mentés..." : "Mentés"}
                      </button>
                    </div>
                  </form>
                )}

                {buildingFloors.length > 0 && (
                  <label className="admin-search-field admin-floor-search">
                    <span>Emelet keresése szám vagy megnevezés alapján</span>
                    <input
                      type="search"
                      value={floorSearch}
                      onChange={(event) => setFloorSearch(event.target.value)}
                      placeholder="Pl. Földszint vagy 2"
                    />
                  </label>
                )}
                {floorSearch.trim() && buildingFloors.length > 0 && (
                  <div className="admin-filter-status">
                    <p className="admin-count" role="status">
                      {matchingFloors.length} / {buildingFloors.length} emelet
                    </p>
                    <button
                      type="button"
                      className="admin-text-button"
                      onClick={() => setFloorSearch("")}
                    >
                      Emeletszűrő törlése
                    </button>
                  </div>
                )}

                <div className="admin-floor-list">
                  {buildingFloors.length === 0 ? (
                    <p className="admin-empty">
                      Ehhez az épülethez még nincs emelet rögzítve.
                    </p>
                  ) : matchingFloors.length === 0 ? (
                    <p className="admin-empty">
                      Nincs a keresésnek megfelelő emelet.
                    </p>
                  ) : (
                    matchingFloors.map((floor) => (
                      <article
                        className={`admin-floor-record admin-record ${
                          selectedFloorId === floor.id ? "selected" : ""
                        }`}
                        key={floor.id}
                      >
                        <button
                          type="button"
                          className="admin-record-select admin-floor-select"
                          aria-pressed={selectedFloorId === floor.id}
                          onClick={() => {
                            setSelectedFloorId(floor.id);
                            setSelectedRoomKey(null);
                          }}
                        >
                          <span
                            className="admin-record-icon"
                            aria-hidden="true"
                          >
                            {floor.number}
                          </span>
                          <span>
                            <strong>{getFloorDisplayName(floor)}</strong>
                            <small>{selectedBuilding?.buildingName}</small>
                          </span>
                        </button>
                        <div className="admin-record-actions">
                          <button
                            type="button"
                            onClick={() => {
                              setSelectedFloorId(floor.id);
                              setSelectedRoomKey(null);
                              setFloorForm({ ...floor });
                            }}
                          >
                            Szerkesztés
                          </button>
                          <button
                            type="button"
                            className="admin-danger-link"
                            onClick={() => deleteFloor(floor)}
                            disabled={saving}
                          >
                            Törlés
                          </button>
                        </div>
                      </article>
                    ))
                  )}
                </div>

                <div className="admin-rooms">
                  <div className="admin-panel-heading">
                    <div>
                      <p className="admin-eyebrow">TEREMTÖRZS</p>
                      <h3>
                        {selectedFloor
                          ? `${getFloorDisplayName(selectedFloor)} termei`
                          : "Termek"}
                      </h3>
                    </div>
                    <div className="admin-form-actions">
                      <span className="admin-count">
                        {roomIsLoading
                          ? "Betöltés..."
                          : `${visibleRooms.length} terem`}
                      </span>
                      <button
                        type="button"
                        className="admin-button admin-button-primary"
                        onClick={() => {
                          setRoomForm({
                            id: "",
                            isEditing: false,
                            floorId: String(selectedFloorId ?? ""),
                            name: "",
                            capacity: "",
                            equipment: "",
                          });
                          setError("");
                        }}
                        disabled={saving || floors.length === 0}
                      >
                        Új terem
                      </button>
                    </div>
                  </div>
                  {roomForm && (
                    <form
                      className="admin-form admin-form-inline"
                      onSubmit={saveRoom}
                    >
                      <h3>
                        {roomForm.isEditing
                          ? "Terem szerkesztése"
                          : "Terem hozzáadása"}
                      </h3>
                      {!roomForm.isEditing && (
                        <label>
                          Teremazonosító
                          <input
                            required
                            maxLength={450}
                            value={roomForm.id}
                            onChange={(event) =>
                              setRoomForm({
                                ...roomForm,
                                id: event.target.value,
                              })
                            }
                            placeholder="Pl. A101"
                          />
                        </label>
                      )}
                      <label>
                        Emelet
                        <select
                          required
                          value={roomForm.floorId}
                          onChange={(event) =>
                            setRoomForm({
                              ...roomForm,
                              floorId: event.target.value,
                            })
                          }
                        >
                          <option value="" disabled>
                            Válassz emeletet
                          </option>
                          {floors.map((floor) => {
                            const building = buildings.find(
                              (item) => item.id === floor.buildingId,
                            );
                            return (
                              <option key={floor.id} value={floor.id}>
                                {building?.buildingName ?? "Ismeretlen épület"}{" "}
                                — {getFloorDisplayName(floor)}
                              </option>
                            );
                          })}
                        </select>
                      </label>
                      <label>
                        Terem neve
                        <input
                          required
                          maxLength={100}
                          value={roomForm.name}
                          onChange={(event) =>
                            setRoomForm({
                              ...roomForm,
                              name: event.target.value,
                            })
                          }
                          placeholder="Pl. 101-es terem"
                        />
                      </label>
                      <label>
                        Férőhely
                        <input
                          required
                          type="number"
                          min="0"
                          step="1"
                          value={roomForm.capacity}
                          onChange={(event) =>
                            setRoomForm({
                              ...roomForm,
                              capacity: event.target.value,
                            })
                          }
                          placeholder="Pl. 30"
                        />
                      </label>
                      <label>
                        Felszerelés
                        <input
                          value={roomForm.equipment}
                          onChange={(event) =>
                            setRoomForm({
                              ...roomForm,
                              equipment: event.target.value,
                            })
                          }
                          placeholder="Pl. projektor, tábla"
                        />
                      </label>
                      <div className="admin-form-actions">
                        <button
                          type="button"
                          className="admin-button admin-button-quiet"
                          onClick={() => setRoomForm(null)}
                          disabled={saving}
                        >
                          Mégse
                        </button>
                        <button
                          type="submit"
                          className="admin-button admin-button-primary"
                          disabled={saving}
                        >
                          {saving ? "Mentés..." : "Mentés"}
                        </button>
                      </div>
                    </form>
                  )}
                  <form className="admin-search-form" onSubmit={searchRooms}>
                    <label className="admin-search-field">
                      <span>
                        Terem szűrése ezen az emeleten; keresés minden emeleten
                      </span>
                      <input
                        type="search"
                        value={roomSearch}
                        disabled={roomSearchLoading}
                        onChange={(event) => {
                          setRoomSearch(event.target.value);
                          setRoomSearchResults(null);
                          setRoomSearchError("");
                        }}
                        placeholder="Név, azonosító, felszerelés vagy épület"
                      />
                    </label>
                    <button
                      type="submit"
                      className="admin-button admin-button-primary"
                      disabled={roomSearchLoading || floors.length === 0}
                    >
                      {roomSearchLoading
                        ? "Keresés..."
                        : "Keresés minden emeleten"}
                    </button>
                    {roomSearch.trim() && (
                      <button
                        type="button"
                        className="admin-button admin-button-quiet"
                        onClick={() => {
                          setRoomSearch("");
                          setRoomSearchResults(null);
                          setRoomSearchError("");
                        }}
                      >
                        Szűrő törlése
                      </button>
                    )}
                  </form>
                  {roomSearchError && (
                    <ErrorToast
                      message={roomSearchError}
                      onDismiss={() => setRoomSearchError("")}
                    />
                  )}
                  {roomSearchResults !== null && (
                    <div className="admin-search-results" aria-live="polite">
                      <p className="admin-count">
                        {roomSearchResults.length} találat az összes emeleten
                      </p>
                      {roomSearchResults.length === 0 ? (
                        <p className="admin-empty">
                          Nincs a keresésnek megfelelő terem.
                        </p>
                      ) : (
                        roomSearchResults.map((room) => (
                          <article
                            className={`admin-room-record admin-record ${
                              selectedRoomKey === `${room.floorId}-${room.id}`
                                ? "selected"
                                : ""
                            }`}
                            key={`${room.floorId}-${room.id}`}
                          >
                            <button
                              type="button"
                              className="admin-room-select"
                              aria-pressed={
                                selectedRoomKey === `${room.floorId}-${room.id}`
                              }
                              onClick={() => {
                                const roomFloor = floors.find(
                                  (floor) => floor.id === room.floorId,
                                );
                                if (roomFloor) {
                                  setSelectedBuildingId(roomFloor.buildingId);
                                }
                                setSelectedFloorId(room.floorId);
                                setSelectedRoomKey(
                                  `${room.floorId}-${room.id}`,
                                );
                              }}
                            >
                              <span
                                className="admin-record-icon"
                                aria-hidden="true"
                              >
                                {room.id.slice(0, 2)}
                              </span>
                              <span className="admin-room-name">
                                <strong>{room.name}</strong>
                                <small>{room.id}</small>
                              </span>
                              <span>{room.capacity} férőhely</span>
                              <span>
                                <small>
                                  {room.building}, {room.floorNumber}
                                </small>
                              </span>
                            </button>
                            <div className="admin-record-actions">
                              <button
                                type="button"
                                onClick={() => editRoom(room)}
                                disabled={saving}
                              >
                                Szerkesztés
                              </button>
                              <button
                                type="button"
                                className="admin-danger-link"
                                onClick={() => deleteRoom(room)}
                                disabled={saving}
                              >
                                Törlés
                              </button>
                            </div>
                          </article>
                        ))
                      )}
                    </div>
                  )}
                  {visibleRoomError && (
                    <ErrorToast
                      message={visibleRoomError}
                      onDismiss={() => setRoomError("")}
                    />
                  )}
                  {!roomIsLoading &&
                    !visibleRoomError &&
                    matchingRooms.length === 0 && (
                      <p className="admin-empty">
                        {visibleRooms.length === 0
                          ? selectedFloor
                            ? "Ezen az emeleten nincs terem."
                            : "Válassz emeletet a termek megtekintéséhez."
                          : "Nincs a szűrőnek megfelelő terem ezen az emeleten."}
                      </p>
                    )}
                  <div className="admin-room-list">
                    {matchingRooms.map((room) => (
                      <article
                        className={`admin-room-record admin-record ${
                          selectedRoomKey === `${selectedFloorId}-${room.id}`
                            ? "selected"
                            : ""
                        }`}
                        key={room.id}
                      >
                        <button
                          type="button"
                          className="admin-room-select"
                          aria-pressed={
                            selectedRoomKey === `${selectedFloorId}-${room.id}`
                          }
                          onClick={() =>
                            setSelectedRoomKey(`${selectedFloorId}-${room.id}`)
                          }
                        >
                          <span
                            className="admin-record-icon"
                            aria-hidden="true"
                          >
                            {room.id.slice(0, 2)}
                          </span>
                          <span className="admin-room-name">
                            <strong>{room.name}</strong>
                            <small>{room.id}</small>
                          </span>
                          <span>{room.capacity} férőhely</span>
                          <span>
                            <small>
                              {room.equipment || "Nincs megadott felszerelés"}
                            </small>
                          </span>
                        </button>
                        <div className="admin-record-actions">
                          <button
                            type="button"
                            onClick={() => editRoom(room)}
                            disabled={saving}
                          >
                            Szerkesztés
                          </button>
                          <button
                            type="button"
                            className="admin-danger-link"
                            onClick={() => deleteRoom(room)}
                            disabled={saving}
                          >
                            Törlés
                          </button>
                        </div>
                      </article>
                    ))}
                  </div>
                </div>
              </>
            )}
          </section>
        </div>
      )}

      {section === "bookings" && (
        <section className="admin-panel admin-bookings-panel">
          <div className="admin-panel-heading">
            <div>
              <p className="admin-eyebrow">FOGLALÁSKÉRÉSEK</p>
              <h2>Foglalások kezelése</h2>
            </div>
            <button
              type="button"
              className="admin-button admin-button-quiet"
              onClick={() => {
                setAdminBookings([]);
                setAdminBookingsError("");
                setAdminBookingsNotice("");
                setBookingRefreshKey((current) => current + 1);
              }}
              disabled={bookingDirectoryLoading || adminBookingsLoading}
            >
              {bookingDirectoryLoading || adminBookingsLoading
                ? "Frissítés..."
                : "Frissítés"}
            </button>
          </div>

          <p className="admin-note">
            Minden foglalás megjelenik. Teremnév, teremazonosító vagy épület
            alapján kereshetsz. A jelenlegi API törlést támogat, jóváhagyást
            vagy státuszmódosítást nem.
          </p>

          {adminBookingsError && (
            <ErrorToast
              message={adminBookingsError}
              onDismiss={() => setAdminBookingsError("")}
            />
          )}
          {bookingRoomError && (
            <ErrorToast
              message={`${bookingRoomError} A foglalások ettől még megtekinthetők teremazonosítóval.`}
              onDismiss={() => setBookingRoomError("")}
            />
          )}
          {adminBookingsNotice && (
            <p className="booking-success" role="status">
              {adminBookingsNotice}
            </p>
          )}

          <label className="admin-search-field admin-booking-search">
            <span>Foglalások keresése terem szerint</span>
            <input
              type="search"
              value={bookingSearch}
              onChange={(event) => setBookingSearch(event.target.value)}
              placeholder="Terem neve, azonosítója vagy épülete"
            />
          </label>

          <p className="admin-count" role="status">
            {adminBookingsLoading
              ? "Foglalások betöltése..."
              : `${matchingAdminBookings.length} / ${adminBookings.length} foglalás`}
          </p>

          {!bookingDirectoryLoading &&
            !adminBookingsLoading &&
            matchingAdminBookings.length === 0 &&
            !adminBookingsError && (
              <p className="admin-empty">
                {adminBookings.length === 0
                  ? "Nincs megjeleníthető foglalás."
                  : "Nincs a teremkeresésnek megfelelő foglalás."}
              </p>
            )}

          {matchingAdminBookings.length > 0 && (
            <div className="booking-list">
              {matchingAdminBookings.map((booking) => {
                const room = roomById[booking.roomId];
                return (
                  <article className="booking-item" key={booking.id}>
                    <div className="booking-item-heading">
                      <h3>
                        {room?.name || `Terem ${booking.roomId}`}{" "}
                        <small>#{booking.id}</small>
                      </h3>
                      <span>{booking.status}</span>
                    </div>
                    <p className="admin-booking-owner">
                      Felhasználó #{booking.userId}
                    </p>
                    <dl>
                      <div>
                        <dt>Kezdés</dt>
                        <dd>{formatBookingDate(booking.startTime)}</dd>
                      </div>
                      <div>
                        <dt>Befejezés</dt>
                        <dd>{formatBookingDate(booking.endTime)}</dd>
                      </div>
                      {booking.specialRequest && (
                        <div>
                          <dt>Megjegyzés</dt>
                          <dd>{booking.specialRequest}</dd>
                        </div>
                      )}
                    </dl>
                    <button
                      type="button"
                      className="admin-button admin-button-danger"
                      onClick={() => deleteAdminBooking(booking)}
                      disabled={deletingBookingId === booking.id}
                    >
                      {deletingBookingId === booking.id
                        ? "Törlés..."
                        : "Foglalás törlése"}
                    </button>
                  </article>
                );
              })}
            </div>
          )}
        </section>
      )}

      {section === "permissions" && (
        <section className="admin-panel admin-unavailable">
          <span className="admin-unavailable-icon" aria-hidden="true">
            API
          </span>
          <div>
            <p className="admin-eyebrow">MESTERKULCSOK</p>
            <h2>Jogosultságok és kiadási napló</h2>
            <p className="admin-note">
              A backendben létezik mesterkulcs-jogosultság adatmodell, de a
              frontend által hívható API nincs hozzá kiépítve.
            </p>
          </div>
        </section>
      )}

      {section === "maintenance" && (
        <section className="admin-panel admin-unavailable">
          <span className="admin-unavailable-icon" aria-hidden="true">
            API
          </span>
          <div>
            <p className="admin-eyebrow">TERMEK LEZÁRÁSA</p>
            <h2>Karbantartási idősávok</h2>
            <p>
              Karbantartás rögzítésére és a foglalások lekérdezésére jelenleg
              nincs backend-végpont.
            </p>
          </div>
        </section>
      )}

      {section === "users" && (
        <section className="admin-panel admin-users-panel">
          <div className="admin-panel-heading">
            <div>
              <p className="admin-eyebrow">FIÓKOK ÉS JOGOSULTSÁGOK</p>
              <h2>Felhasználók kezelése</h2>
            </div>
            <div className="admin-user-heading-actions">
              <button
                type="button"
                className="admin-button admin-button-quiet"
                onClick={async () => {
                  setUsersLoading(true);
                  setRolesLoading(true);
                  setError("");
                  setRolesError("");
                  const [usersResult, rolesResult] = await Promise.allSettled([
                    request("/User"),
                    request("/User/roles"),
                  ]);
                  if (
                    usersResult.status === "fulfilled" &&
                    Array.isArray(usersResult.value)
                  ) {
                    setUsers(usersResult.value);
                  } else {
                    const message =
                      usersResult.status === "rejected"
                        ? usersResult.reason.message
                        : "A felhasználólista formátuma érvénytelen.";
                    setError(
                      `Nem sikerült frissíteni a felhasználókat: ${message}`,
                    );
                  }
                  if (
                    rolesResult.status === "fulfilled" &&
                    Array.isArray(rolesResult.value)
                  ) {
                    setRoles(rolesResult.value);
                  } else {
                    const message =
                      rolesResult.status === "rejected"
                        ? rolesResult.reason.message
                        : "A szerepkörlista formátuma érvénytelen.";
                    setRolesError(
                      `Nem sikerült frissíteni a szerepköröket: ${message}`,
                    );
                  }
                  setUsersLoading(false);
                  setRolesLoading(false);
                }}
                disabled={usersLoading || saving}
              >
                {usersLoading ? "Frissítés..." : "Lista frissítése"}
              </button>
              <button
                type="button"
                className="admin-button admin-button-primary"
                onClick={() => {
                  setSelectedUser(null);
                  setUserForm({ name: "", email: "", password: "" });
                  setError("");
                }}
                disabled={saving}
              >
                Új felhasználó
              </button>
            </div>
          </div>

          <label className="admin-search-field admin-user-search">
            <span>
              Keresés név, e-mail-cím, azonosító vagy szerepkör alapján
            </span>
            <input
              type="search"
              value={userSearch}
              onChange={(event) => setUserSearch(event.target.value)}
              placeholder="Keresés a felhasználók között"
            />
          </label>

          <div className="admin-user-list-heading">
            <p className="admin-count">
              {usersLoading
                ? "Felhasználók betöltése..."
                : `${matchingUsers.length} / ${users.length} felhasználó`}
            </p>
            {userSearch.trim() && (
              <button
                type="button"
                className="admin-text-button"
                onClick={() => setUserSearch("")}
              >
                Keresés törlése
              </button>
            )}
          </div>

          {users.length === 0 && !usersLoading ? (
            <p className="admin-empty">
              Nincs megjeleníthető felhasználó, vagy a lista betöltése nem
              sikerült.
            </p>
          ) : matchingUsers.length === 0 && !usersLoading ? (
            <p className="admin-empty">
              Nincs a keresésnek megfelelő felhasználó.
            </p>
          ) : (
            <div className="admin-user-list">
              {matchingUsers.map((user) => (
                <button
                  type="button"
                  className={`admin-user-list-item ${
                    selectedUser?.id === user.id ? "selected" : ""
                  }`}
                  key={user.id}
                  onClick={() => {
                    setUserForm(null);
                    loadUser(user.id);
                  }}
                  disabled={saving}
                >
                  <span className="admin-user-list-id">#{user.id}</span>
                  <span className="admin-user-list-main">
                    <strong>{user.name}</strong>
                    <small>{user.email}</small>
                  </span>
                  <span
                    className={`admin-user-role ${
                      user.roleId == null ? "unassigned" : ""
                    }`}
                  >
                    {user.roleId == null
                      ? "Nincs szerepkör"
                      : `${getRoleName(roles.find((role) => role.id === user.roleId)?.name)} (#${user.roleId})`}
                  </span>
                </button>
              ))}
            </div>
          )}

          {rolesError && (
            <ErrorToast
              message={rolesError}
              onDismiss={() => setRolesError("")}
            />
          )}

          <form
            className="admin-user-lookup"
            onSubmit={(event) => {
              event.preventDefault();
              const userId = Number(userIdInput);
              if (!Number.isInteger(userId) || userId < 1) {
                setError(
                  "Adj meg egy érvényes, pozitív felhasználó-azonosítót.",
                );
                return;
              }
              loadUser(userId);
            }}
          >
            <label htmlFor="admin-user-id">Felhasználó ID</label>
            <input
              id="admin-user-id"
              type="number"
              min="1"
              step="1"
              required
              value={userIdInput}
              onChange={(event) => setUserIdInput(event.target.value)}
              placeholder="Felhasználó ID"
            />
            <button
              type="submit"
              className="admin-button admin-button-quiet"
              disabled={saving}
            >
              {saving ? "Betöltés..." : "Felhasználó betöltése"}
            </button>
          </form>

          {userForm && (
            <form
              className="admin-form admin-user-form"
              onSubmit={submitUserForm}
            >
              <h3>Új felhasználó létrehozása</h3>
              <p className="admin-note">
                A létrehozás után a felhasználó azonosítójával szerepkört is
                rendelhetsz hozzá. Meglévő fiók adatainak adminisztrátori
                módosításához jelenleg nincs backend API.
              </p>
              <label>
                Név
                <input
                  required
                  maxLength={100}
                  autoComplete="name"
                  value={userForm.name}
                  onChange={(event) =>
                    setUserForm({ ...userForm, name: event.target.value })
                  }
                />
              </label>
              <label>
                E-mail-cím
                <input
                  required
                  type="email"
                  maxLength={254}
                  autoComplete="email"
                  value={userForm.email}
                  onChange={(event) =>
                    setUserForm({ ...userForm, email: event.target.value })
                  }
                />
              </label>
              <label>
                Jelszó
                <input
                  required
                  type="password"
                  autoComplete="new-password"
                  value={userForm.password}
                  onChange={(event) =>
                    setUserForm({ ...userForm, password: event.target.value })
                  }
                />
              </label>
              <div className="admin-form-actions">
                <button
                  type="button"
                  className="admin-button admin-button-quiet"
                  onClick={() => setUserForm(null)}
                  disabled={saving}
                >
                  Mégse
                </button>
                <button
                  type="submit"
                  className="admin-button admin-button-primary"
                  disabled={saving}
                >
                  {saving ? "Mentés..." : "Mentés"}
                </button>
              </div>
            </form>
          )}

          {selectedUser && (
            <article className="admin-user-card">
              <div className="admin-user-card-header">
                <div>
                  <p className="admin-eyebrow">
                    FELHASZNÁLÓ #{selectedUser.id}
                  </p>
                  <h3>{selectedUser.name}</h3>
                  <p>{selectedUser.email}</p>
                </div>
                <div className="admin-record-actions">
                  <button
                    type="button"
                    className="admin-danger-link"
                    onClick={deleteUser}
                    disabled={saving}
                  >
                    Törlés
                  </button>
                </div>
              </div>

              <form className="admin-role-form" onSubmit={assignRole}>
                <label htmlFor="admin-role-id">
                  Szerepkör
                  <select
                    id="admin-role-id"
                    required
                    value={roleIdInput}
                    onChange={(event) => setRoleIdInput(event.target.value)}
                    disabled={saving || rolesLoading}
                  >
                    <option value="">
                      {rolesLoading
                        ? "Szerepkörök betöltése..."
                        : "Válassz szerepkört"}
                    </option>
                    {roles
                      .filter((role) => !isNoRoleName(role.name))
                      .map((role) => (
                        <option key={role.id} value={role.id}>
                          {getRoleName(role.name)} (#{role.id})
                        </option>
                      ))}
                  </select>
                </label>
                <button
                  type="submit"
                  className="admin-button admin-button-primary"
                  disabled={
                    saving ||
                    rolesLoading ||
                    roles.every((role) => isNoRoleName(role.name))
                  }
                >
                  {selectedUserHasNoRole
                    ? "Szerepkör hozzárendelése"
                    : "Szerepkör módosítása"}
                </button>
              </form>
              <p className="admin-role-status">
                {selectedUserHasNoRole
                  ? selectedUser.roleId == null
                    ? "Jelenleg nincs szerepköre."
                    : `Jelenlegi szerepköre: ${getRoleName(selectedRole.name)} – cél szerepkörre állítható.`
                  : `Jelenlegi szerepkör: ${selectedRole ? `${getRoleName(selectedRole.name)} (#${selectedRole.id})` : `#${selectedUser.roleId}`}`}
              </p>
              <button
                type="button"
                className="admin-button admin-button-danger"
                onClick={removeRole}
                disabled={selectedUser.roleId == null || saving}
              >
                Szerepkör törlése
              </button>
            </article>
          )}
        </section>
      )}
    </main>
  );
}
